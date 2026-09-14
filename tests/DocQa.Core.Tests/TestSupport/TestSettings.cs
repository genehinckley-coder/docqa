using DocQa.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace DocQa.Core.Tests.TestSupport;

/// <summary>A complete, valid <c>DocQa</c> configuration section, as flat configuration keys.</summary>
internal static class TestSettings
{
    public static Dictionary<string, string?> Valid()
    {
        Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DocQa:CorpusDirectory"] = "corpus",
            ["DocQa:ApiKeyPath"] = "key.txt",
            ["DocQa:StateFilePath"] = "state.json",
            ["DocQa:TokenCeiling"] = "400000",
            ["DocQa:ContextWindow"] = "1000000",
            ["DocQa:ContextWarnFraction"] = "0.7",
            ["DocQa:CacheReadToleranceFraction"] = "0.01",
            ["DocQa:MinCitationLength"] = "8",
            ["DocQa:ThinCoverageFraction"] = "0.5",
        };

        foreach (EffortLevel level in CacheAffecting.EffortLevels)
        {
            settings[$"DocQa:MaxTokens:{level}"] = "32000";
            settings[$"DocQa:RequestTimeouts:{level}"] = "00:20:00";
            settings[$"DocQa:EstimatedOutputTokens:{level}:Lowest"] = "1000";
            settings[$"DocQa:EstimatedOutputTokens:{level}:Highest"] = "5000";
        }

        foreach (SelectableModel model in CacheAffecting.SelectableModels)
        {
            settings[$"DocQa:Prices:{model.Id}:InputPerMTok"] = "5";
            settings[$"DocQa:Prices:{model.Id}:CacheWrite5mPerMTok"] = "6.25";
            settings[$"DocQa:Prices:{model.Id}:CacheWrite1hPerMTok"] = "10";
            settings[$"DocQa:Prices:{model.Id}:CacheReadPerMTok"] = "0.5";
            settings[$"DocQa:Prices:{model.Id}:OutputPerMTok"] = "25";
        }

        return settings;
    }

    public static IConfiguration ToConfiguration(IEnumerable<KeyValuePair<string, string?>> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    public static DocQaOptions ValidOptions() => Bind(ToConfiguration(Valid()));

    public static DocQaOptions Bind(IConfiguration configuration) =>
        configuration.GetSection(DocQaOptions.SectionName).Get<DocQaOptions>()
        ?? throw new InvalidOperationException("The DocQa section is missing.");
}
