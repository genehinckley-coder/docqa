using System.Reflection;
using Bunit;
using DocQa.Web.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace DocQa.Web.Tests;

/// <summary>
/// The three screens exist at the routes the nav rail links to. Item 10 fills in Corpus settings; the Conversation and
/// Documents screens have their own tests in <see cref="ConversationPageTests"/> and <see cref="DocumentsPageTests"/>.
/// </summary>
public sealed class PageTests : IDisposable
{
    private readonly BunitContext _context = new();

    public void Dispose() => _context.Dispose();

    [Fact]
    public void TheScreens_AreWhereTheRailPoints()
    {
        Assert.Equal("/", Route<Conversation>());
        Assert.Equal("/documents", Route<Documents>());
        Assert.Equal("/settings", Route<CorpusSettings>());
    }

    [Fact]
    public void TheScreenNotYetBuilt_NamesItself() =>
        Assert.Equal("Corpus settings", _context.Render<CorpusSettings>().Find("h1").TextContent);

    private static string? Route<TPage>() => typeof(TPage).GetCustomAttribute<RouteAttribute>()?.Template;
}
