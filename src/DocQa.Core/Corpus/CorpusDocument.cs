using DocQa.Core.Answering;
using DocQa.Core.Text;

namespace DocQa.Core.Corpus;

/// <summary>
/// One document of a <see cref="Corpus"/> snapshot: its text exactly as it is sent, and what the quote
/// checker and the document viewer need to work against that text.
/// </summary>
public sealed class CorpusDocument
{
    // The extractor separates paragraphs with exactly one blank line, and a paragraph never contains one.
    private const string ParagraphSeparator = "\n\n";

    internal CorpusDocument(string fileName, RequestDocument request, long? approximateTokens)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(request);

        FileName = fileName;
        Request = request;
        CodePoints = CodePointIndex.Build(request.Text);
        Paragraphs = FindParagraphs(request.Text);
        ApproximateTokens = approximateTokens;
    }

    /// <summary>The corpus file name, such as <c>ADMINISTRATIVE CODE 41 175.htm</c>.</summary>
    public string FileName { get; }

    /// <summary>The document exactly as it is rendered into a request's document block.</summary>
    public RequestDocument Request { get; }

    /// <summary>The citation label: the document block's <c>title</c>.</summary>
    public string Label => Request.Title;

    /// <summary>The document text. Citation offsets index into exactly this string, in code points.</summary>
    public string Text => Request.Text;

    /// <summary>Converts the code-point offsets citations use into offsets into <see cref="Text"/>.</summary>
    public CodePointIndex CodePoints { get; }

    /// <summary>The text's length in code points, the unit citations use and every displayed count uses.</summary>
    public int CharacterCount => CodePoints.Length;

    /// <summary>
    /// Each paragraph's range in <see cref="Text"/>, in UTF-16 offsets and in order. The blank line between
    /// two paragraphs belongs to neither.
    /// </summary>
    public IReadOnlyList<TextRange> Paragraphs { get; }

    /// <summary>
    /// This document's tokens when counted on its own, or <see langword="null"/> when the count failed.
    /// Approximate: documents counted separately do not sum to the corpus total. For display only.
    /// </summary>
    public long? ApproximateTokens { get; }

    internal static IReadOnlyList<TextRange> FindParagraphs(string text)
    {
        List<TextRange> ranges = [];
        int start = 0;
        while (true)
        {
            int separator = text.IndexOf(ParagraphSeparator, start, StringComparison.Ordinal);
            int end = separator < 0 ? text.Length : separator;
            if (end > start)
            {
                ranges.Add(new TextRange(start, end));
            }

            if (separator < 0)
            {
                return ranges.AsReadOnly();
            }

            start = separator + ParagraphSeparator.Length;
        }
    }
}
