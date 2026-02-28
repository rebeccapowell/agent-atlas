using Atlas.Host.Models;
using Atlas.Host.Services;
using Moq;
using Xunit;

namespace Atlas.Host.Tests;

public class ToolIndexTests
{
    private static IReadOnlyList<ToolDefinition> SampleTools() =>
    [
        new ToolDefinition
        {
            ToolId = "customers-read",
            ApiId = "customers-api",
            DisplayName = "Read Customers",
            Summary = "Returns a list of customers",
            Description = "Retrieves customers matching the given query",
            Method = "GET",
            Path = "/customers",
            Safety = "read",
            Tags = ["customers", "read"],
        },
        new ToolDefinition
        {
            ToolId = "orders-create",
            ApiId = "orders-api",
            DisplayName = "Create Order",
            Summary = "Creates a new order",
            Description = "Places a new order in the system",
            Method = "POST",
            Path = "/orders",
            Safety = "write",
            Tags = ["orders", "write"],
        },
    ];

    [Fact]
    public void Search_NoFilters_ReturnsAllTools()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var results = index.Search(null, null, null, null);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_ByQuery_ReturnsMatchingTools()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var results = index.Search("customer", null, null, null);

        Assert.Single(results);
        Assert.Equal("customers-read", results[0].ToolId);
    }

    [Fact]
    public void Search_ByApiId_FiltersCorrectly()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var results = index.Search(null, "orders-api", null, null);

        Assert.Single(results);
        Assert.Equal("orders-create", results[0].ToolId);
    }

    [Fact]
    public void Search_BySafety_FiltersCorrectly()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var results = index.Search(null, null, null, "read");

        Assert.Single(results);
        Assert.Equal("customers-read", results[0].ToolId);
    }

    [Fact]
    public void Search_ByTags_FiltersCorrectly()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var results = index.Search(null, null, ["write"], null);

        Assert.Single(results);
        Assert.Equal("orders-create", results[0].ToolId);
    }

    [Fact]
    public void GetById_ExistingId_ReturnsTool()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var tool = index.GetById("customers-read");

        Assert.NotNull(tool);
        Assert.Equal("Read Customers", tool.DisplayName);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var loader = new Mock<ICatalogLoader>();
        loader.Setup(l => l.GetTools()).Returns(SampleTools());
        var index = new ToolIndex(loader.Object);

        var tool = index.GetById("does-not-exist");

        Assert.Null(tool);
    }
}
