using System.Reflection;
using DocQa.Web.Components.Pages;
using DocQa.Web.Tests.Conversations;
using DocQa.Web.Tests.Settings;
using DocQa.Web.Tests.Viewer;
using Microsoft.AspNetCore.Components;

namespace DocQa.Web.Tests;

/// <summary>
/// The three screens exist at the routes the nav rail links to. Each has its own tests in
/// <see cref="ConversationPageTests"/>, <see cref="DocumentsPageTests"/> and <see cref="CorpusSettingsPageTests"/>.
/// </summary>
public sealed class PageTests
{
    [Fact]
    public void TheScreens_AreWhereTheRailPoints()
    {
        Assert.Equal("/", Route<Conversation>());
        Assert.Equal("/documents", Route<Documents>());
        Assert.Equal("/settings", Route<CorpusSettings>());
    }

    private static string? Route<TPage>() => typeof(TPage).GetCustomAttribute<RouteAttribute>()?.Template;
}
