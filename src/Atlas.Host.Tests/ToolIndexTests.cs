using Atlas.Host.Models;
using Atlas.Host.Services;
using NSubstitute;
using Shouldly;
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
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var results = index.Search(null, null, null, null);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void Search_ByQuery_ReturnsMatchingTools()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var results = index.Search("customer", null, null, null);

        results.ShouldHaveSingleItem();
        results[0].ToolId.ShouldBe("customers-read");
    }

    [Fact]
    public void Search_ByApiId_FiltersCorrectly()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var results = index.Search(null, "orders-api", null, null);

        results.ShouldHaveSingleItem();
        results[0].ToolId.ShouldBe("orders-create");
    }

    [Fact]
    public void Search_BySafety_FiltersCorrectly()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var results = index.Search(null, null, null, "read");

        results.ShouldHaveSingleItem();
        results[0].ToolId.ShouldBe("customers-read");
    }

    [Fact]
    public void Search_ByTags_FiltersCorrectly()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var results = index.Search(null, null, ["write"], null);

        results.ShouldHaveSingleItem();
        results[0].ToolId.ShouldBe("orders-create");
    }

    [Fact]
    public void GetById_ExistingId_ReturnsTool()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var tool = index.GetById("customers-read");

        tool.ShouldNotBeNull();
        tool.DisplayName.ShouldBe("Read Customers");
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var loader = Substitute.For<ICatalogLoader>();
        loader.GetTools().Returns(SampleTools());
        var index = new ToolIndex(loader);

        var tool = index.GetById("does-not-exist");

        tool.ShouldBeNull();
    }
}
