using Bunit;
using DocQa.Web.Components.Pages;

namespace DocQa.Web.Tests;

/// <summary>
/// A smoke test that proves bUnit runs under xunit.v3 4.x. The template page it renders is replaced
/// by the Conversation screen in plan item 8.
/// </summary>
public sealed class HomePageTests
{
    [Fact]
    public void TheHomePage_Renders()
    {
        using var context = new BunitContext();

        IRenderedComponent<Home> page = context.Render<Home>();

        page.Find("h1").MarkupMatches("<h1>Hello, world!</h1>");
    }
}
