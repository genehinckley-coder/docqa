using System.Reflection;
using Bunit;
using DocQa.Web.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace DocQa.Web.Tests;

/// <summary>
/// The three screens exist at the routes the nav rail links to. Items 9 and 10 fill in the last two; the
/// Conversation screen's own tests are in <see cref="ConversationPageTests"/>.
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
    public void EachScreen_NamesItself()
    {
        Assert.Equal("Documents", _context.Render<Documents>().Find("h1").TextContent);
        Assert.Equal("Corpus settings", _context.Render<CorpusSettings>().Find("h1").TextContent);
    }

    private static string? Route<TPage>() => typeof(TPage).GetCustomAttribute<RouteAttribute>()?.Template;
}
