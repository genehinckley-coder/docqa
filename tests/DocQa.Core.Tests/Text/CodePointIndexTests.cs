using DocQa.Core.Tests.TestSupport;
using DocQa.Core.Text;

namespace DocQa.Core.Tests.Text;

/// <summary>Plan item 1, check 3: code-point offsets, converted to UTF-16, slice the right span.</summary>
public sealed class CodePointIndexTests
{
    private const string WithNonBmp = "ab" + SpikeFixtures.NonBmp + "cd";

    [Fact]
    public void TextWithoutSurrogatePairs_IsTheIdentity()
    {
        const string text = "Section\u00A0175.410 § — sumps";
        CodePointIndex index = CodePointIndex.Build(text);

        Assert.True(index.IsIdentity);
        Assert.Equal(text.Length, index.Length);
        for (int offset = 0; offset <= text.Length; offset++)
        {
            Assert.Equal(offset, index.ToUtf16(offset));
        }
    }

    [Fact]
    public void ANonBmpCharacter_ShiftsEveryLaterOffsetByOne()
    {
        CodePointIndex index = CodePointIndex.Build(WithNonBmp);

        Assert.False(index.IsIdentity);
        Assert.Equal(5, index.Length);
        Assert.Equal([0, 1, 2, 4, 5, 6], Enumerable.Range(0, 6).Select(index.ToUtf16));
        Assert.Equal("cd", Slice(WithNonBmp, index, 3, 5));
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData(WithNonBmp)]
    public void TheExclusiveEndAtTheTextsLength_Converts(string text)
    {
        CodePointIndex index = CodePointIndex.Build(text);

        Assert.Equal(text.Length, index.ToUtf16(index.Length));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void OffsetsOutsideTheText_AreRefused(int offset)
    {
        // 6 is the UTF-16 length, but only 5 code points: bounds are checked in code points.
        CodePointIndex index = CodePointIndex.Build(WithNonBmp);

        Assert.Throws<ArgumentOutOfRangeException>(() => index.ToUtf16(offset));
    }

    [Fact]
    public void ALoneSurrogate_CountsAsOneCodePoint()
    {
        // A lone low surrogate, then a well-formed pair.
        string text = "a\uDC00" + SpikeFixtures.NonBmp + "b";
        CodePointIndex index = CodePointIndex.Build(text);

        Assert.Equal(4, index.Length);
        Assert.Equal(4, index.ToUtf16(3));
        Assert.Equal(5, index.ToUtf16(4));
    }

    [Fact]
    public void AnEmptyText_HasOnlyTheEndOffset()
    {
        CodePointIndex index = CodePointIndex.Build("");

        Assert.Equal(0, index.Length);
        Assert.Equal(0, index.ToUtf16(0));
    }

    [Fact]
    public void InTheSpikeFixture_ACodePointSpanAfterTheNonBmpCharacter_SlicesTheQuotedText()
    {
        string text = SpikeFixtures.TextA;
        const string quote = "minimum capacity of\n5\u00A0gallons";
        int utf16Start = text.IndexOf(quote, StringComparison.Ordinal);
        int start = text[..utf16Start].EnumerateRunes().Count();
        int end = start + quote.EnumerateRunes().Count();

        // The fixture's one non-BMP character comes before the quote, so the two offsets differ.
        Assert.Equal(utf16Start - 1, start);
        Assert.Equal(quote, Slice(text, CodePointIndex.Build(text), start, end));
    }

    private static string Slice(string text, CodePointIndex index, int start, int end)
    {
        int from = index.ToUtf16(start);
        return text[from..index.ToUtf16(end)];
    }
}
