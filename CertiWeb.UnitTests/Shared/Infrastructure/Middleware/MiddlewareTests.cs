using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace CertiWeb.UnitTests.Shared.Infrastructure.Middleware;

/// <summary>
/// Unit tests for middleware components and HTTP pipeline
/// </summary>
public class MiddlewareTests
{
    #region Error Handling Middleware Tests

    [Test]
    public async Task ErrorHandlingMiddleware_WhenNoException_ShouldContinuePipeline()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(200, context.Response.StatusCode);
    }

    [Test]
    public async Task ErrorHandlingMiddleware_WhenArgumentException_ShouldReturn400()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware(context => 
            throw new ArgumentException("Invalid argument"));
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(400, context.Response.StatusCode);
    }

    [Test]
    public async Task ErrorHandlingMiddleware_WhenUnauthorizedException_ShouldReturn401()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware(context => 
            throw new UnauthorizedAccessException("Unauthorized"));
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(401, context.Response.StatusCode);
    }

    [Test]
    public async Task ErrorHandlingMiddleware_WhenNotFoundException_ShouldReturn404()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware(context => 
            throw new FileNotFoundException("Resource not found"));
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(404, context.Response.StatusCode);
    }

    [Test]
    public async Task ErrorHandlingMiddleware_WhenGenericException_ShouldReturn500()
    {
        // Arrange
        var middleware = new ErrorHandlingMiddleware(context => 
            throw new Exception("Internal server error"));
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(500, context.Response.StatusCode);
    }

    [Test]
    public async Task ErrorHandlingMiddleware_WhenException_ShouldLogError()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            context => throw new Exception("Test exception"),
            loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Test exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Request Logging Middleware Tests

    [Test]
    public async Task RequestLoggingMiddleware_WhenRequest_ShouldLogRequestDetails()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            async context => await Task.CompletedTask,
            loggerMock.Object);
        var context = CreateHttpContext("GET", "/api/cars");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GET") && v.ToString().Contains("/api/cars")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RequestLoggingMiddleware_WhenLongRequest_ShouldLogDuration()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            async context => await Task.Delay(100),
            loggerMock.Object);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("completed") && v.ToString().Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Validation Middleware Tests

    [Test]
    public async Task ValidationMiddleware_WhenValidRequest_ShouldContinuePipeline()
    {
        // Arrange
        var middleware = new ValidationMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext("POST", "/api/cars");
        context.Request.Body = CreateJsonBody(new { Model = "Test", Year = 2020, Price = 25000 });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(200, context.Response.StatusCode);
    }

    [Test]
    public async Task ValidationMiddleware_WhenInvalidJson_ShouldReturn400()
    {
        // Arrange
        var middleware = new ValidationMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext("POST", "/api/cars");
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("invalid json"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(400, context.Response.StatusCode);
    }

    [Test]
    public async Task ValidationMiddleware_WhenMissingRequiredFields_ShouldReturn400()
    {
        // Arrange
        var middleware = new ValidationMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext("POST", "/api/cars");
        context.Request.Body = CreateJsonBody(new { Model = "" }); // Missing required fields

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(400, context.Response.StatusCode);
    }

    #endregion

    #region Rate Limiting Middleware Tests

    [Test]
    public async Task RateLimitingMiddleware_WhenUnderLimit_ShouldAllowRequest()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(
            async context => await Task.CompletedTask,
            maxRequestsPerMinute: 100);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(200, context.Response.StatusCode);
    }

    [Test]
    public async Task RateLimitingMiddleware_WhenOverLimit_ShouldReturn429()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(
            async context => await Task.CompletedTask,
            maxRequestsPerMinute: 1);
        var context = CreateHttpContext();

        // Act - Make multiple requests
        await middleware.InvokeAsync(context);
        context = CreateHttpContext(); // Create new context for second request
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(429, context.Response.StatusCode);
    }

    [Test]
    public async Task RateLimitingMiddleware_WhenOverLimit_ShouldIncludeRetryAfterHeader()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(
            async context => await Task.CompletedTask,
            maxRequestsPerMinute: 1);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        context = CreateHttpContext();
        await middleware.InvokeAsync(context);

        // Assert
        Assert.IsTrue(context.Response.Headers.ContainsKey("Retry-After"));
    }

    #endregion

    #region Security Headers Middleware Tests

    [Test]
    public async Task SecurityHeadersMiddleware_WhenRequest_ShouldAddSecurityHeaders()
    {
        // Arrange
        var middleware = new SecurityHeadersMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.IsTrue(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.IsTrue(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.IsTrue(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.IsTrue(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Test]
    public async Task SecurityHeadersMiddleware_WhenRequest_ShouldSetCorrectHeaderValues()
    {
        // Arrange
        var middleware = new SecurityHeadersMiddleware(async context => await Task.CompletedTask);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.AreEqual("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.AreEqual("1; mode=block", context.Response.Headers["X-XSS-Protection"]);
        StringAssert.Contains("max-age=", context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    #endregion

    #region CORS Middleware Tests

    [Test]
    public async Task CorsMiddleware_WhenValidOrigin_ShouldAllowRequest()
    {
        // Arrange
        var allowedOrigins = new[] { "https://example.com" };
        var middleware = new CorsMiddleware(
            async context => await Task.CompletedTask,
            allowedOrigins);
        var context = CreateHttpContext();
        context.Request.Headers.Add("Origin", "https://example.com");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual("https://example.com", context.Response.Headers["Access-Control-Allow-Origin"]);
    }

    [Test]
    public async Task CorsMiddleware_WhenInvalidOrigin_ShouldNotSetCorsHeaders()
    {
        // Arrange
        var allowedOrigins = new[] { "https://example.com" };
        var middleware = new CorsMiddleware(
            async context => await Task.CompletedTask,
            allowedOrigins);
        var context = CreateHttpContext();
        context.Request.Headers.Add("Origin", "https://malicious.com");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.IsFalse(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Test]
    public async Task CorsMiddleware_WhenPreflightRequest_ShouldReturn200()
    {
        // Arrange
        var allowedOrigins = new[] { "https://example.com" };
        var middleware = new CorsMiddleware(
            async context => await Task.CompletedTask,
            allowedOrigins);
        var context = CreateHttpContext("OPTIONS", "/api/cars");
        context.Request.Headers.Add("Origin", "https://example.com");
        context.Request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.AreEqual(200, context.Response.StatusCode);
        Assert.IsTrue(context.Response.Headers.ContainsKey("Access-Control-Allow-Methods"));
    }

    #endregion

    #region Helper Methods

    private static HttpContext CreateHttpContext(string method = "GET", string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Stream CreateJsonBody(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    #endregion
}

#region Mock Middleware Classes

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware>? _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware>? logger = null)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException)
        {
            context.Response.StatusCode = 400;
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = 401;
        }
        catch (FileNotFoundException)
        {
            context.Response.StatusCode = 404;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            context.Response.StatusCode = 500;
        }
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Request {Method} {Path} started", 
            context.Request.Method, context.Request.Path);

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation("Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
            context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
    }
}

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                var document = JsonDocument.Parse(body);
                // Basic validation - check if required fields exist
                if (!IsValidRequest(document, context.Request.Path))
                {
                    context.Response.StatusCode = 400;
                    return;
                }
            }
            catch (JsonException)
            {
                context.Response.StatusCode = 400;
                return;
            }
        }

        await _next(context);
    }

    private static bool IsValidRequest(JsonDocument document, string path)
    {
        if (path.Contains("/cars"))
        {
            return document.RootElement.TryGetProperty("Model", out var model) && 
                   !string.IsNullOrEmpty(model.GetString());
        }
        return true;
    }
}

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequestsPerMinute;
    private static readonly Dictionary<string, List<DateTime>> _requests = new();

    public RateLimitingMiddleware(RequestDelegate next, int maxRequestsPerMinute)
    {
        _next = next;
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        if (!_requests.ContainsKey(clientId))
        {
            _requests[clientId] = new List<DateTime>();
        }

        var clientRequests = _requests[clientId];
        clientRequests.RemoveAll(r => r < now.AddMinutes(-1));

        if (clientRequests.Count >= _maxRequestsPerMinute)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            return;
        }

        clientRequests.Add(now);
        await _next(context);
    }
}

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        await _next(context);
    }
}

public class CorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _allowedOrigins;

    public CorsMiddleware(RequestDelegate next, string[] allowedOrigins)
    {
        _next = next;
        _allowedOrigins = allowedOrigins;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].FirstOrDefault();

        if (!string.IsNullOrEmpty(origin) && _allowedOrigins.Contains(origin))
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        }

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            context.Response.StatusCode = 200;
            return;
        }

        await _next(context);
    }
}

#endregion
