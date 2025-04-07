using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageServer;

/// <summary>
/// Entry point for the Image Server application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        // Optimize thread pool settings
        ThreadPool.SetMinThreads(100, 100);

        var baseDir = Directory.GetCurrentDirectory();
        var port = 23564;

        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options =>
                    {
                        options.Limits.MaxConcurrentConnections = 1000;
                        options.Limits.MaxRequestBodySize = null; // No request size limit
                        options.Listen(IPAddress.Any, port);
                    })
                    .UseStartup<Startup>();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(baseDir);
                services.AddMemoryCache(options =>
                {
                    options.SizeLimit = 200 * 1024 * 1024; // 200MB cache limit
                });
                services.AddSingleton<ImageService>();
                services.AddCors();
            })
            .Build()
            .Run();
    }
}

/// <summary>
/// Configures the application's HTTP pipeline.
/// </summary>
public class Startup
{
    /// <summary>
    /// Configures the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The hosting environment.</param>
    /// <param name="imageService">The image service.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ImageService imageService)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting();

        // Configure CORS
        app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/{**path}", async context => { await imageService.ServeImageAsync(context); });
        });

        Console.WriteLine($"Server started on http://*:{23564}/");
        Console.WriteLine($"Serving files from: {imageService.BaseDirectory}");
        Console.WriteLine("Press Ctrl+C to stop the server...");
    }
}

/// <summary>
/// Service responsible for serving image files with caching capabilities.
/// </summary>
public class ImageService
{
    // Only cache files smaller than this size to avoid memory pressure
    private const int MaxCacheFileSize = 5 * 1024 * 1024; // 5MB

    // Cache duration
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private readonly IMemoryCache _cache;
    
    /// <summary>
    /// Gets the base directory from which files are served.
    /// </summary>
    public readonly string BaseDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache to use.</param>
    /// <param name="baseDirectory">The base directory to serve files from.</param>
    public ImageService(IMemoryCache cache, string baseDirectory)
    {
        _cache = cache;
        BaseDirectory = Path.GetFullPath(baseDirectory);
        Directory.CreateDirectory(BaseDirectory); // Ensure directory exists
    }

    /// <summary>
    /// Serves an image file in response to an HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ServeImageAsync(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            // Get requested path
            var requestedPath = request.Path.Value?.TrimStart('/') ?? string.Empty;
            

            if (string.IsNullOrEmpty(requestedPath))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Construct and validate file path (prevent directory traversal attacks)
            var fullPath = Path.GetFullPath(Path.Combine(BaseDirectory, requestedPath));
            if (!fullPath.StartsWith(BaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            // Console.WriteLine($"Base Directory: {BaseDirectory}");
            // Console.WriteLine($"Requested File Path: {fullPath}");
            // Console.WriteLine($"File Exists: {File.Exists(fullPath)}");
            // Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            // Check if file exists
            if (!File.Exists(fullPath))
            {
                
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // Get file information
            var fileInfo = new FileInfo(fullPath);
            var extension = Path.GetExtension(fullPath).ToLower();
            var mimeType = GetMimeType(extension);

            // Set Content-Type
            response.ContentType = mimeType;

            // Set cache control headers
            var lastModified = fileInfo.LastWriteTimeUtc;
            var etag = $"\"{lastModified.Ticks:X}-{fileInfo.Length:X}\"";

            response.Headers["ETag"] = etag;
            response.Headers["Last-Modified"] = lastModified.ToString("R");
            response.Headers["Cache-Control"] = "public, max-age=86400"; // Client cache for 1 day

            // Check conditional request (304 Not Modified handling)
            var ifNoneMatch = request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag)
            {
                response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }

            // Cache key
            var cacheKey = $"file:{fullPath}:{lastModified.Ticks}";

            // Determine if file size is suitable for caching
            if (fileInfo.Length <= MaxCacheFileSize)
            {
                // Try to get from cache
                if (!_cache.TryGetValue(cacheKey, out byte[]? cachedContent))
                {
                    // Cache miss, read file content
                    cachedContent = await File.ReadAllBytesAsync(fullPath);

                    // Set cache options
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSize(cachedContent.Length) // Set cache item size
                        .SetAbsoluteExpiration(CacheDuration);

                    // Store in cache
                    _cache.Set(cacheKey, cachedContent, cacheOptions);
                }

                // Send file from cache
                if (cachedContent != null)
                {
                    response.ContentLength = cachedContent.Length;
                    await response.Body.WriteAsync(cachedContent);
                }
            }
            else
            {
                // Stream large files directly, without caching
                response.ContentLength = fileInfo.Length;
                await using var fileStream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024, // 64KB buffer
                    true);

                await fileStream.CopyToAsync(response.Body);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serving image: {ex.Message}");

            if (!response.HasStarted) response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }

    /// <summary>
    /// Gets the MIME type for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <returns>The corresponding MIME type.</returns>
    private string GetMimeType(string extension)
    {
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
