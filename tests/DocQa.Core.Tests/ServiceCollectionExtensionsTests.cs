using DocQa.Core.Configuration;
using DocQa.Core.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocQa.Core.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private static readonly string ContentRoot = Path.Combine(Path.GetTempPath(), "docqa-content-root");

    [Fact]
    public void RelativePaths_ResolveAgainstTheContentRoot()
    {
        using ServiceProvider services = Build(TestSettings.Valid());

        DocQaOptions options = services.GetRequiredService<IOptions<DocQaOptions>>().Value;

        Assert.Equal(Path.Combine(ContentRoot, "corpus"), options.CorpusDirectory);
        Assert.Equal(Path.Combine(ContentRoot, "key.txt"), options.ApiKeyPath);
        Assert.Equal(Path.Combine(ContentRoot, "state.json"), options.StateFilePath);
    }

    [Fact]
    public void StartupValidation_IsWiredIn_AndReportsACrossCheckFailure()
    {
        Dictionary<string, string?> settings = TestSettings.Valid();
        settings.Remove($"DocQa:MaxTokens:{EffortLevel.XHigh}");
        using ServiceProvider services = Build(settings);

        // IStartupValidator is what the host runs at startup for ValidateOnStart.
        var ex = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("MaxTokens has no value for the effort level XHigh", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupValidation_ReportsADataAnnotationFailure()
    {
        Dictionary<string, string?> settings = TestSettings.Valid();
        settings["DocQa:ApiKeyPath"] = "";
        using ServiceProvider services = Build(settings);

        var ex = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(nameof(DocQaOptions.ApiKeyPath), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheClock_IsTheSystemClock()
    {
        using ServiceProvider services = Build(TestSettings.Valid());

        Assert.Same(TimeProvider.System, services.GetRequiredService<TimeProvider>());
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddDocQaCore(TestSettings.ToConfiguration(settings), ContentRoot)
            .BuildServiceProvider();
}
