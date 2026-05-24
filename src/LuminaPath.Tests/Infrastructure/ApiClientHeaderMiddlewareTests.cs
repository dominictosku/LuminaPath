using LuminaPath.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;

namespace LuminaPath.Tests.Infrastructure;

public class ApiClientHeaderMiddlewareTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task InvokeAsync_AllowsSafeApiRequestsWithoutClientHeader(string method)
    {
        var nextCalled = false;
        var middleware = new ApiClientHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext(method, "/api/quests");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_RejectsUnsafeApiRequestsWithoutClientHeader()
    {
        var nextCalled = false;
        var middleware = new ApiClientHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/api/quests");

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowsUnsafeApiRequestsWithExpectedClientHeader()
    {
        var nextCalled = false;
        var middleware = new ApiClientHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("PATCH", "/api/quests/1");
        context.Request.Headers[ApiClientHeaderMiddleware.HeaderName] = ApiClientHeaderMiddleware.HeaderValue;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotRequireHeaderForBrowserAccountForms()
    {
        var nextCalled = false;
        var middleware = new ApiClientHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/Account/Register");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
