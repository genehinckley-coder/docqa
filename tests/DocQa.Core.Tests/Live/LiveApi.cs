using Anthropic;
using Anthropic.Models.Messages;
using DocQa.Core.ApiClient;
using DocQa.Core.Answering;
using DocQa.Core.Configuration;
using DocQa.Core.Tests.TestSupport;
using Microsoft.Extensions.Configuration;

namespace DocQa.Core.Tests.Live;

/// <summary>
/// Support for billed tests against the real Messages API. They are skipped unless
/// <c>DOCQA_LIVE=1</c>, and they run with the production options from the web app's
/// <c>appsettings.json</c>: the real key file, and each level's <c>max_tokens</c> and timeout.
/// </summary>
internal static class LiveApi
{
    public const string Variable = "DOCQA_LIVE";

    // Below its model's minimum a cache breakpoint is silently ignored, and every caching check would
    // fail for a fixture reason.
    private static readonly Dictionary<string, long> MinimumCacheablePrefix = new(StringComparer.Ordinal)
    {
        [ModelIds.ClaudeOpus5] = 512,
        [ModelIds.ClaudeSonnet5] = 1_024,
    };

    private static readonly Lock LogLock = new();
    private static decimal _totalCost;

    public static DocQaOptions Options { get; } = TestSettings.Bind(new ConfigurationBuilder()
        .AddJsonFile(RepoPaths.Combine("src", "DocQa.Web", "appsettings.json"), optional: false, reloadOnChange: false)
        .Build());

    private static string LogPath { get; } = RepoPaths.Combine("TestResults", "item1-live.log");

    public static void RequireEnabled() =>
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable(Variable) == "1",
            $"Billed live API test; set {Variable}=1 to run it.");

    public static AnthropicClientProvider CreateProvider(CaptureHandler capture) =>
        AnthropicClientProvider.Create(ApiKeyLoader.Load(Options.ApiKeyPath), Options.RequestTimeouts, [capture]);

    /// <summary>
    /// The production system prompt plus a per-run id. The id makes each run start cold, so a rerun
    /// within the hour cannot turn an expected cache write into a read. It is exactly the kind of
    /// variation the production prompt must never contain.
    /// </summary>
    public static string NewSystemPrompt() => $"{CacheAffecting.SystemPrompt}\n\nSpike run id: {Guid.NewGuid():N}";

    public static MessageCreateParams Request(
        string modelId, EffortLevel level, string systemPrompt, IReadOnlyList<MessageParam> messages) =>
        MessageRequestBuilder.Create(modelId, level, Options.MaxTokens[level], systemPrompt, messages);

    /// <summary>Two free <c>count_tokens</c> calls, before any billed one.</summary>
    public static async Task AssertFixturesCacheableAsync(IAnthropicClient client, string systemPrompt, CancellationToken ct)
    {
        foreach (SelectableModel model in CacheAffecting.SelectableModels)
        {
            MessageTokensCount count = await client.Messages.CountTokens(
                new MessageCountTokensParams
                {
                    Model = model.Id,
                    System = systemPrompt,
                    Messages = [MessageRequestBuilder.FirstTurn(SpikeFixtures.Documents, "?")],
                },
                ct);

            Log($"fixture prefix on {model.Id}: {count.InputTokens:N0} tokens (cacheable minimum {MinimumCacheablePrefix[model.Id]:N0})");
            Assert.True(
                count.InputTokens > MinimumCacheablePrefix[model.Id],
                $"The fixtures count {count.InputTokens} tokens on {model.Id}, not above its cacheable minimum. Enlarge them.");
        }
    }

    /// <summary>The most one live test run may spend; after it, further billed calls are refused.</summary>
    public const decimal RunBudget = 3.00m;

    /// <summary>
    /// The longest one live request may run. At the measured ~95 output tokens a second, it bounds a
    /// runaway answer at about 17,000 tokens (about $0.45 on Opus 5). <c>max_tokens</c> keeps each level's
    /// production value, because check 7 must send 64,000 and 128,000.
    /// </summary>
    public static readonly TimeSpan RequestCap = TimeSpan.FromMinutes(3);

    /// <summary>
    /// <see cref="SinglePass.StreamAsync"/>, refused once the run has spent <see cref="RunBudget"/>, and
    /// stopped if it runs past <see cref="RequestCap"/>. Cancelling <paramref name="ct"/> still surfaces as
    /// an <see cref="OperationCanceledException"/>, so the Stop test is unaffected.
    /// </summary>
    public static async Task<(Message Message, List<string> EventNames)> StreamAsync(
        IAnthropicClient client,
        MessageCreateParams parameters,
        CancellationToken ct,
        Action<RawMessageStreamEvent>? onEvent = null)
    {
        lock (LogLock)
        {
            if (_totalCost >= RunBudget)
            {
                throw new InvalidOperationException(
                    $"This live run has spent ${_totalCost:F2}, reaching its ${RunBudget:F2} budget; further billed calls are refused.");
            }
        }

        using var cap = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cap.CancelAfter(RequestCap);
        try
        {
            return await SinglePass.StreamAsync(client, parameters, cap.Token, onEvent);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Log($"a request ran past the {RequestCap.TotalMinutes:F0}-minute cap and was stopped; its cost is not counted");
            throw new TimeoutException($"A live request ran past the spike's {RequestCap.TotalMinutes:F0}-minute cap and was stopped.");
        }
    }

    public static decimal Cost(string modelId, Usage usage)
    {
        ModelPrices prices = Options.Prices[modelId];
        CacheCreation? creation = usage.CacheCreation;

        // Without a TTL breakdown the whole write is priced at the 1-hour rate, so the figure errs high.
        long write1h = creation?.Ephemeral1hInputTokens ?? usage.CacheCreationInputTokens ?? 0;
        long write5m = creation?.Ephemeral5mInputTokens ?? 0;
        return (usage.InputTokens * prices.InputPerMTok
            + write1h * prices.CacheWrite1hPerMTok
            + write5m * prices.CacheWrite5mPerMTok
            + (usage.CacheReadInputTokens ?? 0) * prices.CacheReadPerMTok
            + usage.OutputTokens * prices.OutputPerMTok) / 1_000_000m;
    }

    /// <summary>Logs one response's usage and cost, and adds the cost to the run's total.</summary>
    public static void Record(string label, string modelId, EffortLevel level, Message message, TimeSpan elapsed)
    {
        Usage usage = message.Usage;
        decimal cost = Cost(modelId, usage);
        lock (LogLock)
        {
            _totalCost += cost;
        }

        int thinkingBlocks = message.Content.Count(block => block.TryPickThinking(out _));
        Log(
            $"{label}: {modelId} {level.ToWireValue()} {elapsed.TotalSeconds:F0}s stop={message.StopReason?.Raw()} " +
            $"in={usage.InputTokens:N0} write1h={usage.CacheCreation?.Ephemeral1hInputTokens:N0} " +
            $"write5m={usage.CacheCreation?.Ephemeral5mInputTokens:N0} read={usage.CacheReadInputTokens ?? 0:N0} " +
            $"out={usage.OutputTokens:N0} thinking-blocks={thinkingBlocks} ${cost:F4}");
    }

    public static void LogTotal()
    {
        lock (LogLock)
        {
            Log($"running total this test run: ${_totalCost:F4}");
        }
    }

    /// <summary>Writes to the test output and to <c>TestResults/item1-live.log</c> (gitignored).</summary>
    public static void Log(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
        lock (LogLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} {line}\n");
        }
    }
}
