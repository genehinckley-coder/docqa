using DocQa.Core.ApiClient;
using DocQa.Core.Configuration;
using DocQa.Core.Corpus;
using DocQa.Core.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocQa.Core.Tests.Live;

/// <summary>
/// Plan item 3, against the real API: the C# request counts exactly what the prototype's did. Free
/// (<c>count_tokens</c> is not billed), but skipped unless <c>DOCQA_LIVE=1</c>, as every live test is.
/// </summary>
/// <remarks>
/// In the non-parallel collection, because another live test sets process-wide ANTHROPIC_* variables that
/// a client built here could read.
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CorpusTokenCountLiveTests
{
    [Fact]
    public async Task TheAssembledCorpus_CountsExactlyThePrototypesTotal_OnEveryModel()
    {
        LiveApi.RequireEnabled();
        var capture = new CaptureHandler();
        using AnthropicClientProvider provider = LiveApi.CreateProvider(capture);
        var loader = new CorpusLoader(new ApiTokenCounter(() => provider), NullLogger<CorpusLoader>.Instance);

        DocQa.Core.Corpus.Corpus corpus = await loader.AssembleAsync(
            CorpusExtractor.ExtractCorpus(RepoPaths.Combine("admincode")),
            LiveApi.Options.TokenCeiling,
            TestContext.Current.CancellationToken);

        foreach (SelectableModel model in CacheAffecting.SelectableModels)
        {
            LiveApi.Log($"corpus on {model.Id}: {corpus.TokenCounts[model.Id]:N0} tokens (prototype on {BlocksGolden.PrototypeTokensModel}: {BlocksGolden.PrototypeTokens:N0})");
        }

        foreach (CorpusDocument document in corpus.Documents)
        {
            LiveApi.Log($"  {document.FileName}: about {document.ApproximateTokens:N0} tokens alone");
        }

        Assert.All(CacheAffecting.SelectableModels, model => Assert.Equal(BlocksGolden.PrototypeTokens, corpus.TokenCounts[model.Id]));
        Assert.All(corpus.Documents, document => Assert.NotNull(document.ApproximateTokens));
        Assert.Equal(CacheAffecting.SelectableModels.Count + corpus.Documents.Count, capture.Requests.Count);
    }
}
