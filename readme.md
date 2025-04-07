# ImageServer 轻量级高性能图片服务器 | Lightweight High-Performance Image Server

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

ImageServer 是一个基于 ASP.NET Core 开发的轻量级、高性能图片服务器，专为快速提供静态图片资源而设计，具有内存缓存和高并发处理能力。

ImageServer is a lightweight, high-performance image server built on ASP.NET Core, specifically designed for fast delivery of static image resources with memory caching and high concurrency capabilities.

## 特性 | Features

- **高性能** - 针对图片服务进行优化，支持高并发请求
  
  **High Performance** - Optimized for image serving with high concurrency support

- **内存缓存** - 自动缓存小型图片文件，提高访问速度
  
  **Memory Caching** - Automatically caches small image files for faster access

- **HTTP 缓存支持** - 支持 ETag 和 Last-Modified 缓存控制
  
  **HTTP Caching Support** - Implements ETag and Last-Modified cache control

- **CORS 支持** - 内置跨域资源共享支持
  
  **CORS Support** - Built-in Cross-Origin Resource Sharing

- **安全防护** - 防止目录遍历攻击
  
  **Security Protection** - Prevents directory traversal attacks

- **适配多种图片格式** - 支持 JPEG, PNG, GIF, BMP, WebP 等常见格式
  
  **Multiple Image Format Support** - Handles JPEG, PNG, GIF, BMP, WebP and more

- **智能资源管理** - 大文件流式传输，小文件缓存，优化内存使用
  
  **Smart Resource Management** - Streams large files, caches small ones for optimal memory usage

## 快速开始 | Quick Start

### 安装要求 | Requirements

- .NET 9.0 SDK 或更高版本 | .NET 9.0 SDK or higher

### 运行 | Running

```bash
# 克隆仓库 | Clone the repository
git clone https://github.com/duya25446/ImageServer.git
cd ImageServer

# 构建并运行 | Build and run
dotnet build
dotnet run
```
也可以直接下载Github中的预编译好的Release版本，直接使用对应操作系统的运行方式即可
Alternatively, you can directly download the precompiled Release version from GitHub and simply use the corresponding execution method for your operating system.

服务器默认在 http://*:23564/ 启动，并使用应用程序目录作为图片目录。

The server starts at http://*:23564/ by default and uses the application directory as the image directory.

## 使用方式 | Usage

### 访问图片 | Accessing Images

将图片文件放在服务器的基础目录中，然后通过 URL 访问：

Place image files in the server's base directory and access them via URL:

```
http://localhost:23564/path/to/image.jpg
```

### 示例 | Example

如果你有以下文件结构：

If you have the following file structure:

```
/app_directory/
  └── images/
      ├── logo.png
      └── photos/
          └── vacation.jpg
```

你可以通过以下 URL 访问这些图片：

You can access these images via:

```
http://localhost:23564/images/logo.png
http://localhost:23564/images/photos/vacation.jpg
```

## 配置 | Configuration

在 `Program.cs` 中可调整以下配置：

The following configurations can be adjusted in `Program.cs`:

- 端口号（默认：23564）| Port number (default: 23564)
- 最大并发连接数（默认：1000）| Maximum concurrent connections (default: 1000)
- 缓存大小（默认：200MB）| Cache size (default: 200MB)
- 线程池设置（默认：最小100线程）| Thread pool settings (default: min 100 threads)

在 `ImageService` 类中可调整：

In class `ImageService` you can adjust:

- 最大缓存文件大小（默认：5MB）| Maximum cacheable file size (default: 5MB)
- 缓存持续时间（默认：30分钟）| Cache duration (default: 30 minutes)
- 客户端缓存时间（默认：1天）| Client-side cache time (default: 1 day)

## ImageServer 更新记录

### 版本 1.0.1 (2025-04-07)
#### 修复
- 修复单文件部署时的路径解析问题
- 添加条件编译宏定义区分开发和发布环境
- 更改Release模式使用`Directory.GetCurrentDirectory()`获取当前工作目录
- 解决发布模式下的404错误问题

#### 技术细节
```csharp
#if DEBUG
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
#else
    var baseDir = Directory.GetCurrentDirectory();
#endif
```

### 版本 1.0.0 (初始版本)
#### 功能
- 实现基本的图片服务功能
- 支持通过HTTP请求访问服务器上的图片文件
- 简单的路由解析和文件提供机制

#### 已知问题
- 发布版本无法正确解析文件路径
- 在单文件部署环境中路径解析异常
- Release版本运行时静态文件404错误

---

*注: 版本1.0.1修复了初始版本中的所有关键路径解析问题。*

## 高级特性 | Advanced Features

### 缓存管理 | Cache Management

ImageServer 使用两级缓存策略：

ImageServer uses a two-tier caching strategy:

1. **服务器内存缓存** - 小于 5MB 的文件会被缓存在服务器内存中 30 分钟

   **Server Memory Cache** - Files smaller than 5MB are cached in server memory for 30 minutes

2. **客户端浏览器缓存** - 通过 HTTP 缓存控制头指示浏览器缓存内容 1 天

   **Client Browser Cache** - HTTP cache control headers instruct browsers to cache content for 1 day

### 高并发处理 | High Concurrency Handling

- 优化的线程池配置确保高并发性能
  
  Optimized thread pool configuration ensures high concurrency performance

- Kestrel 服务器配置为处理最多 1000 个并发连接
  
  Kestrel server configured to handle up to 1000 concurrent connections

- 大文件使用 64KB 缓冲区的流式传输，避免内存压力
  
  Large files are streamed with 64KB buffer to avoid memory pressure

## 性能指标 | Performance Metrics

- TODO 
>我不是测试工程师，不知道如何对本做标准的基准性能测试，本项目的性能目前都只来源于原理和理论推算，如果你有测试的能力和条件的话，欢迎贡献！
I am not a test engineer and do not know how to conduct standard benchmark performance tests. The performance of this project is currently derived solely from principles and theoretical estimation. If you possess the capability and resources for testing, your contributions are most welcome!

## 许可证 | License

本项目采用 MIT 许可证。

This project is licensed under the MIT License 

## 联系方式 | Contact

如有问题或建议，请通过 Issues 页面联系我。

For questions or suggestions, please contact me through the Issues page.

---

**ImageServer** - 简单高效的图片服务解决方案 | A simple and efficient image serving solution.