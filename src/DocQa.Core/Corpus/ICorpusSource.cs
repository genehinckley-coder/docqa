namespace DocQa.Core.Corpus;

/// <summary>
/// The app's current corpus state, as the conversation store and the UI read it. Implemented by
/// <see cref="CorpusService"/>; tests substitute their own.
/// </summary>
public interface ICorpusSource
{
    /// <summary>The current state. Each read returns an immutable snapshot.</summary>
    CorpusState State { get; }

    /// <summary>
    /// Raised after <see cref="State"/> changes, on whichever thread changed it. Handlers read
    /// <see cref="State"/>, which may already be newer than the change that raised the notification.
    /// </summary>
    event Action? StateChanged;
}
