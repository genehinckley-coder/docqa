using AngleSharp.Dom;
using Bunit;
using DocQa.Core.Answering;
using DocQa.Core.Conversations;
using DocQa.Core.Quotes;
using DocQa.Web.Components.Conversations;
using DocQa.Web.Components.Ds;
using DocQa.Web.Tests.TestSupport;

namespace DocQa.Web.Tests.Conversations;

public sealed class AnswerViewTests : IDisposable
{
    private const string Payload = "<script>alert(1)</script>";
    private const string BaseUri = "http://localhost/?c=1";

    private readonly BunitContext _context = new();

    public void Dispose() => _context.Dispose();

    [Fact]
    public void AMatchedSource_ShowsItsLabelRangeQuoteAndStatus()
    {
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed());

        IElement source = cut.Find(".qa__source");
        Assert.Equal("answer-0-source-1", source.Id);
        Assert.Equal($"Source 1: {TestData.Label} · characters 36–65", source.QuerySelector(".qa__source-label")!.TextContent);
        Assert.Equal($"“{TestData.CitedText}”", source.QuerySelector(".qa__source-quote")!.TextContent);
        IElement badge = source.QuerySelector(".d1-badge")!;
        Assert.Contains("d1-badge--success", badge.ClassList);
        Assert.Equal("matched", badge.TextContent.Trim());
        Assert.Null(source.QuerySelector(".qa__source-detail"));
        Assert.Equal("1 of 1 quotes matched; 0% of the answer's text cites nothing.", cut.Find(".qa__quality").TextContent);
    }

    [Theory]
    [InlineData(MatchStatus.Mismatch, "mismatch", "d1-badge--danger")]
    [InlineData(MatchStatus.OutOfRange, "out of range", "d1-badge--warning")]
    [InlineData(MatchStatus.Degenerate, "degenerate", "d1-badge--warning")]
    [InlineData(MatchStatus.NotCheckable, "not checkable", "d1-badge--warning")]
    public void AFailedSource_ShowsItsStatusInWords_AndItsDetail(MatchStatus status, string label, string toneClass)
    {
        var check = new QuoteCheck(TestData.Citation(), "char_location", TestData.CitedText, status, "The quote departs from the span at character 4.");
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(quotes: TestData.Report(check)));

        IElement badge = cut.Find(".qa__source .d1-badge");
        Assert.Equal(label, badge.TextContent.Trim());
        Assert.Contains(toneClass, badge.ClassList);
        Assert.Equal("The quote departs from the span at character 4.", cut.Find(".qa__source-detail").TextContent);
        Assert.Equal("0 of 1 quotes matched; 0% of the answer's text cites nothing.", cut.Find(".qa__quality").TextContent);
    }

    [Fact]
    public void AnUnreadableCitation_IsLabeledAsSuch()
    {
        var check = new QuoteCheck(null, "page_location", "quoted", MatchStatus.NotCheckable, "Not a char_location citation.");
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(quotes: TestData.Report(check)));

        Assert.Contains("Not a readable citation (page_location)", cut.Find(".qa__source-label").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void EachMarker_LinksToItsSource_AndIsNamedForAScreenReader()
    {
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed());

        IElement marker = cut.Find("a.answer-text__marker");
        Assert.Equal("[1]", marker.TextContent);
        Assert.Equal(BaseUri + "#answer-0-source-1", marker.GetAttribute("href"));
        Assert.Equal($"Source 1: {TestData.Label} · characters 36–65", marker.GetAttribute("aria-label"));
    }

    [Fact]
    public void TheSourcesLabel_ExplainsProvenanceNotEntailment() =>
        Assert.Equal("Provenance, not entailment", Render(TestData.Completed()).FindComponent<Tooltip>().Instance.Label);

    [Fact]
    public void ThinlySourcedBlocks_AreListed()
    {
        QuoteReport report = TestData.Report(TestData.Matched()) with
        {
            CitedBlocks = [new BlockCoverage(TestData.AnswerText, [1], 28, 7, 0.5)],
        };

        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(quotes: report));

        Assert.Equal(
            $"“{TestData.AnswerText}” [1]: 25% of its text is in matched quotes.",
            cut.Find(".qa__thin-list li").TextContent);
    }

    [Theory]
    [InlineData(TurnWarningKind.Truncated, "TRUNCATED", "d1-badge--danger")]
    [InlineData(TurnWarningKind.Refused, "REFUSED", "d1-badge--danger")]
    [InlineData(TurnWarningKind.CacheMiss, "CACHE MISS", "d1-badge--danger")]
    [InlineData(TurnWarningKind.NotCached, "NOT CACHED", "d1-badge--danger")]
    [InlineData(TurnWarningKind.Context, "CONTEXT", "d1-badge--warning")]
    [InlineData(TurnWarningKind.Incomplete, "INCOMPLETE", "d1-badge--warning")]
    public void EachWarning_HasItsBadge_AndItsTextWithoutRepeatingIt(TurnWarningKind kind, string label, string toneClass)
    {
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(warnings: [new TurnWarning(kind, label + ": the details.")]));

        IElement warning = cut.Find(".qa__warning");
        IElement badge = warning.QuerySelector(".d1-badge")!;
        Assert.Equal(label, badge.TextContent.Trim());
        Assert.Contains(toneClass, badge.ClassList);
        Assert.Equal("the details.", warning.QuerySelector(":scope > span:last-child")!.TextContent);
    }

    [Fact]
    public void TheDisclaimer_IsShownOnlyWhenAsked()
    {
        Assert.Equal(QuoteChecker.MatchDisclaimer, Render(TestData.Completed(), disclaimer: true).Find(".qa__disclaimer").TextContent);
        Assert.Empty(Render(TestData.Completed(), disclaimer: false).FindAll(".qa__disclaimer"));
    }

    [Fact]
    public void TheMetaLine_NamesTheSettingsCostCacheTimeAndTokens()
    {
        AnswerRecord record = TestData.Completed();

        Assert.Equal(
            $"Opus 5, High · {Formatting.Money(record.Result!.Cost.Total)} · cold cache write · 58 s · 199,777 input and 5,313 output tokens",
            Render(record).Find(".qa__meta").TextContent);
    }

    [Fact]
    public void AStoppedTurn_KeepsItsPartialAnswer_Labeled_WithWhatItCost()
    {
        IRenderedComponent<AnswerView> cut = Render(TestData.Stopped());

        Assert.Equal("A sump must", cut.Find(".answer-text").TextContent.Trim());
        IElement notice = cut.Find(".qa__notice");
        Assert.Equal("STOPPED", notice.QuerySelector(".d1-badge")!.TextContent.Trim());
        Assert.Contains("Stopped: not checked, not added to the conversation.", notice.TextContent, StringComparison.Ordinal);
        Assert.Equal("Opus 5, High · Input billed about $2.00; output before Stop not reported.", cut.Find(".qa__meta").TextContent);
        Assert.Empty(cut.FindAll(".qa__sources"));
    }

    [Fact]
    public void AStopBeforeAnyText_SaysSo()
    {
        AnswerRecord record = TestData.Stopped() with { Segments = [], Cost = null, CostNote = "Cost unknown: up to about $2.00 if the request had started." };

        IRenderedComponent<AnswerView> cut = Render(record);

        Assert.Equal("Stopped before any answer text arrived.", cut.Find(".qa__empty").TextContent);
        Assert.Contains("Cost unknown: up to about $2.00", cut.Find(".qa__meta").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AFailedTurn_ShowsItsNotice()
    {
        AnswerRecord record = TestData.Stopped() with
        {
            Outcome = TurnOutcome.Failed,
            Notice = "The API could not be reached. Not added to the conversation.",
        };

        IElement notice = Render(record).Find(".qa__notice");

        Assert.Contains("d1-badge--danger", notice.QuerySelector(".d1-badge")!.ClassList);
        Assert.Equal("FAILED", notice.QuerySelector(".d1-badge")!.TextContent.Trim());
    }

    [Fact]
    public void TheThinkingSummary_IsCollapsedIntoADisclosure()
    {
        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(thinking: "Section 175.410 applies."));

        IElement details = cut.Find("details.qa__reasoning");
        Assert.False(details.HasAttribute("open"));
        Assert.Equal("Reasoning summary", details.QuerySelector("summary")!.TextContent);
        Assert.Equal("Section 175.410 applies.", details.QuerySelector(".qa__reasoning-text")!.TextContent);
    }

    [Fact]
    public void ModelAndDocumentText_IsEncoded_NeverMarkup()
    {
        var check = new QuoteCheck(TestData.Citation(Payload), "char_location", Payload, MatchStatus.Mismatch, Payload);
        AnswerRecord record = TestData.Completed(question: Payload, text: Payload, quotes: TestData.Report(check), thinking: Payload) with
        {
            Notice = Payload,
            Warnings = [new TurnWarning(TurnWarningKind.Refused, Payload)],
        };

        IRenderedComponent<AnswerView> cut = Render(record);

        Assert.Empty(cut.FindAll("script"));
        Assert.Contains("&lt;script&gt;", cut.Markup, StringComparison.Ordinal);
    }

    // --- The click-through to the document (plan item 9) ---------------------------------------------

    [Fact]
    public void EachSource_LinksToItsSpanOnTheDocumentsScreen()
    {
        Guid conversation = Guid.NewGuid();

        IRenderedComponent<AnswerView> cut = Render(TestData.Completed(), conversation: conversation);

        IElement link = cut.Find(".qa__source-link");
        Assert.Equal($"documents?c={conversation}&answer=0&source=1", link.GetAttribute("href"));
        Assert.Equal("Show in document, source 1", link.TextContent);
    }

    [Fact]
    public void WithoutAConversation_ASourceHasNoLinkToTheDocument() =>
        Assert.Empty(Render(TestData.Completed()).FindAll(".qa__source-link"));

    [Fact]
    public void ASourceWithNothingToShow_HasNoLinkToTheDocument()
    {
        var outOfRange = new QuoteCheck(
            new AnswerCitation(0, 500, 600, TestData.CitedText, TestData.Label), "char_location", TestData.CitedText, MatchStatus.OutOfRange, "Outside the text.");
        var unreadable = new QuoteCheck(null, "page_location", "quoted", MatchStatus.NotCheckable, "Not a char_location citation.");

        IRenderedComponent<AnswerView> cut = Render(
            TestData.Completed(quotes: TestData.Report(outOfRange, unreadable)), conversation: Guid.NewGuid());

        Assert.Equal(2, cut.FindAll(".qa__source").Count);
        Assert.Empty(cut.FindAll(".qa__source-link"));
    }

    private IRenderedComponent<AnswerView> Render(AnswerRecord record, bool disclaimer = false, Guid? conversation = null) =>
        _context.Render<AnswerView>(p => p
            .Add(x => x.Record, record)
            .Add(x => x.Index, 0)
            .Add(x => x.ShowDisclaimer, disclaimer)
            .Add(x => x.BaseUri, BaseUri)
            .Add(x => x.ConversationId, conversation));
}
