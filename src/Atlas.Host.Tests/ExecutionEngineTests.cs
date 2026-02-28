using Atlas.Host.Configuration;
using Atlas.Host.Execution;
using Atlas.Host.Models;
using Atlas.Host.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Atlas.Host.Tests;

public class ExecutionEngineTests
{
    // ---------- helpers ----------

    private static IOptions<AtlasOptions> DefaultOptions(Action<ExecLimitsOptions>? configure = null)
    {
        var opts = new AtlasOptions();
        configure?.Invoke(opts.ExecLimits);
        return Options.Create(opts);
    }

    private static IToolIndex ToolIndexWith(params ToolDefinition[] tools)
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(tools);
        return new ToolIndex(loader);
    }

    private static ICatalogLoader CatalogWith(params ApiEntry[] apis)
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetApis().Returns(apis);
        return loader;
    }

    private static JsonElement ParsePlan(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ---------- ExecContext limits ----------

    [Fact]
    public void CheckResponseSize_PerResponseExceeded_ThrowsLimit()
    {
        var limits = new ExecLimitsOptions { MaxResponseBytes = 100, MaxBytes = 10_000 };
        var ctx = new ExecContext(false, limits, "tok", null);

        var ex = Should.Throw<ExecutionLimitException>(() => ctx.CheckResponseSize(101));
        ex.Message.ShouldContain("100");
    }

    [Fact]
    public void CheckResponseSize_TotalExceeded_ThrowsLimit()
    {
        var limits = new ExecLimitsOptions { MaxResponseBytes = 1000, MaxBytes = 150 };
        var ctx = new ExecContext(false, limits, "tok", null);

        // First call is fine (90 bytes < 1000 per-response, 90 < 150 total)
        ctx.CheckResponseSize(90);

        // Second call: cumulative = 90 + 90 = 180 > MaxBytes (150)
        var ex = Should.Throw<ExecutionLimitException>(() => ctx.CheckResponseSize(90));
        ex.Message.ShouldContain("150");
    }

    [Fact]
    public void CheckResponseSize_BothWithinLimits_DoesNotThrow()
    {
        var limits = new ExecLimitsOptions { MaxResponseBytes = 1000, MaxBytes = 10_000 };
        var ctx = new ExecContext(false, limits, "tok", null);

        Should.NotThrow(() => ctx.CheckResponseSize(500));
    }

    // ---------- Plan format ----------

    [Fact]
    public async Task ExecuteAsync_UndefinedPlan_ReturnsFailure()
    {
        var engine = new ExecutionEngine(
            ToolIndexWith(),
            CatalogWith(),
            DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var result = await engine.ExecuteAsync(default, "dryRun", null, "tok");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownStepType_ReturnsFailure()
    {
        var engine = new ExecutionEngine(
            ToolIndexWith(),
            CatalogWith(),
            DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"unknown"}]""");
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
        result.Error!.ShouldContain("unknown");
    }

    [Fact]
    public async Task ExecuteAsync_PlanWithStepsWrapper_ExecutesSuccessfully()
    {
        // Verifies {"steps":[...]} root format is unwrapped correctly
        var tool = new ToolDefinition
        {
            ToolId = "test-tool",
            ApiId = "test-api",
            Method = "GET",
            Path = "/items",
        };
        var api = new ApiEntry { ApiId = "test-api", BaseUrl = "https://example.com" };

        var toolIndex = ToolIndexWith(tool);
        var catalog = CatalogWith(api);

        var engine = new ExecutionEngine(
            toolIndex, catalog, DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""{"steps":[{"type":"call","toolId":"test-tool","args":{}}]}""");
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeTrue();
        result.Steps.ShouldNotBeNull();
        result.Steps!.Length.ShouldBe(1);
    }

    // ---------- DryRun ----------

    [Fact]
    public async Task ExecuteAsync_DryRun_ReturnsStepsWithoutHttpCalls()
    {
        var tool = new ToolDefinition
        {
            ToolId = "sample-tool",
            ApiId = "sample-api",
            Method = "GET",
            Path = "/data",
        };
        var api = new ApiEntry { ApiId = "sample-api", BaseUrl = "https://api.example.com" };

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"sample-tool","args":{}}]""");
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeTrue();
        result.Mode.ShouldBe("dryRun");
        result.Steps.ShouldNotBeNull();
        result.Steps!.Length.ShouldBe(1);
        result.Steps[0].DryRun.ShouldBeTrue();
        result.Steps[0].ToolId.ShouldBe("sample-tool");
    }

    // ---------- URL construction ----------

    [Fact]
    public async Task ExecuteAsync_DryRun_UrlBuiltWithUriBuilder()
    {
        // Base URL with trailing slash should not produce double-slash
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/customers",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://api.example.com/" };

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"t1","args":{}}]""");
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeTrue();
        result.Steps![0].Url.ShouldNotContain("//customers");
        result.Steps[0].Url.ShouldBe("https://api.example.com/customers");
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_QueryParamsEncoded()
    {
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/search",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://api.example.com" };

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"t1","args":{"q":"hello world"}}]""");
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeTrue();
        result.Steps![0].Url.ShouldContain("q=hello%20world");
    }

    // ---------- Step limits ----------

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxSteps_ReturnsLimitError()
    {
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/x",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://example.com" };

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(l => l.MaxSteps = 1),
            Substitute.For<IHttpClientFactory>(),
            NullLogger<ExecutionEngine>.Instance);

        // Two steps exceeds limit of 1
        var plan = ParsePlan("""
            [
                {"type":"call","toolId":"t1"},
                {"type":"call","toolId":"t1"}
            ]
            """);
        var result = await engine.ExecuteAsync(plan, "dryRun", null, "tok");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
        result.Error!.ShouldContain("steps");
    }

    // ---------- foreach with JsonElement array ----------

    [Fact]
    public async Task ExecuteAsync_Foreach_IteratesJsonElementArray()
    {
        // After a 'call', the saved variable is a JsonElement array — foreach must handle it
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/items",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://example.com" };
        var factory = Substitute.For<IHttpClientFactory>();
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"id":1},{"id":2}]""", Encoding.UTF8, "application/json")
            });
        var client = new HttpClient(handler);
        factory.CreateClient("atlas-exec").Returns(client);

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            factory,
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""
        [
            {"type":"call","toolId":"t1","saveAs":"items"},
            {"type":"foreach","items":"items","as":"item","do":[
                {"type":"return","from":"item"}
            ]}
        ]
        """);
        // Should not throw; iterating a JsonElement array should work
        var result = await engine.ExecuteAsync(plan, "run", null, "tok");

        result.Success.ShouldBeTrue();
    }

    // ---------- HTTP error sanitization ----------

    [Fact]
    public async Task ExecuteAsync_DownstreamError_DoesNotLeakBody()
    {
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/secret",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://example.com" };
        var factory = Substitute.For<IHttpClientFactory>();
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("stack trace with secret internal details", Encoding.UTF8, "text/plain")
            });
        var client = new HttpClient(handler);
        factory.CreateClient("atlas-exec").Returns(client);

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            factory,
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"t1"}]""");
        var result = await engine.ExecuteAsync(plan, "run", null, "tok");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
        // Error message must NOT contain the raw downstream body
        result.Error!.ShouldNotContain("stack trace");
        result.Error.ShouldNotContain("secret internal details");
        result.Error.ShouldContain("500");
    }

    [Fact]
    public async Task ExecuteAsync_DownstreamError_StepRecordContainsError()
    {
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/items",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://example.com" };
        var factory = Substitute.For<IHttpClientFactory>();
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found", Encoding.UTF8, "text/plain")
            });
        factory.CreateClient("atlas-exec").Returns(new HttpClient(handler));

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(),
            factory,
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"t1"}]""");
        var result = await engine.ExecuteAsync(plan, "run", null, "tok");

        result.Steps.ShouldNotBeNull();
        result.Steps![0].Error.ShouldNotBeNullOrEmpty();
        result.Steps[0].StatusCode.ShouldBe(404);
    }

    // ---------- Response size limit (bytes, not chars) ----------

    [Fact]
    public async Task ExecuteAsync_ResponseExceedsPerResponseLimit_ReturnsLimitError()
    {
        var tool = new ToolDefinition
        {
            ToolId = "t1",
            ApiId = "a1",
            Method = "GET",
            Path = "/big",
        };
        var api = new ApiEntry { ApiId = "a1", BaseUrl = "https://example.com" };
        var factory = Substitute.For<IHttpClientFactory>();
        var bigBody = new string('x', 200); // 200 bytes
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(bigBody, Encoding.UTF8, "text/plain")
            });
        factory.CreateClient("atlas-exec").Returns(new HttpClient(handler));

        var engine = new ExecutionEngine(
            ToolIndexWith(tool),
            CatalogWith(api),
            DefaultOptions(l => { l.MaxResponseBytes = 100; l.MaxBytes = 10_000; }),
            factory,
            NullLogger<ExecutionEngine>.Instance);

        var plan = ParsePlan("""[{"type":"call","toolId":"t1"}]""");
        var result = await engine.ExecuteAsync(plan, "run", null, "tok");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
        result.Error!.ShouldContain("100");
    }
}

/// <summary>Minimal fake HttpMessageHandler for unit tests.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}
