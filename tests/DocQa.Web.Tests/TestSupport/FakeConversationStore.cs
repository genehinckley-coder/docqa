using DocQa.Core.Answering;
using DocQa.Core.Conversations;

namespace DocQa.Web.Tests.TestSupport;

/// <summary>
/// A conversation store whose snapshots the test sets. <see cref="Stop"/> behaves as the real store's does; the
/// members the UI shell does not use throw.
/// </summary>
internal sealed class FakeConversationStore : IConversationStore
{
    private readonly Dictionary<Guid, ConversationSnapshot> _snapshots = [];

    public event Action<Guid>? Changed;

    public ConversationCost TotalCost { get; private set; } = ConversationCost.Zero;

    /// <summary>Every call to <see cref="Stop"/>, in order.</summary>
    public List<Guid> StopCalls { get; } = [];

    public int SubscriberCount => Changed?.GetInvocationList().Length ?? 0;

    public IReadOnlyList<ConversationSnapshot> List() => [.. _snapshots.Values];

    public ConversationSnapshot? Get(Guid id) => _snapshots.GetValueOrDefault(id);

    public ConversationSnapshot Create(AnswerSettings? settings = null) => throw new NotSupportedException();

    public bool TryChangeSettings(Guid id, AnswerSettings settings) => throw new NotSupportedException();

    public AskResult Ask(Guid id, string question) => throw new NotSupportedException();

    public string? TakeReturnedQuestion(Guid id) => throw new NotSupportedException();

    /// <summary>As the real store: marks the answer in flight as stopping and raises the change; otherwise a no-op.</summary>
    public void Stop(Guid id)
    {
        StopCalls.Add(id);
        if (Get(id) is { InProgress: { IsStopping: false } inProgress } snapshot)
        {
            Publish(snapshot with { InProgress = inProgress with { IsStopping = true } });
        }
    }

    /// <summary>Replaces a conversation's snapshot and raises its change.</summary>
    public void Publish(ConversationSnapshot snapshot)
    {
        _snapshots[snapshot.Id] = snapshot;
        Changed?.Invoke(snapshot.Id);
    }

    /// <summary>Replaces a conversation's snapshot without raising its change, as if the event were still on its way.</summary>
    public void Replace(ConversationSnapshot snapshot) => _snapshots[snapshot.Id] = snapshot;

    /// <summary>Sets the total and raises a change, as any turn's progress does.</summary>
    public void PublishTotal(ConversationCost total)
    {
        TotalCost = total;
        Changed?.Invoke(Guid.NewGuid());
    }

    public static ConversationSnapshot Idle(Guid id) => new()
    {
        Id = id,
        Title = "New conversation",
        CreatedAt = DateTimeOffset.UnixEpoch,
        Settings = AnswerSettings.Default,
        SettingsLocked = false,
        Corpus = null,
        Messages = [],
        Answers = [],
        InProgress = null,
        LastOutcome = null,
        ReturnedQuestion = null,
        Cost = ConversationCost.Zero,
        LastRequestTokens = null,
    };

    public static ConversationSnapshot Finished(Guid id, TurnOutcome outcome) => Idle(id) with { LastOutcome = outcome };

    public static ConversationSnapshot Answering(Guid id, bool stopping = false) => Idle(id) with
    {
        SettingsLocked = true,
        InProgress = new InProgressAnswer
        {
            Question = "What are the sump requirements?",
            Settings = AnswerSettings.Default,
            AskedAt = DateTimeOffset.UnixEpoch,
            Segments = [],
            Thinking = "",
            HasRedactedThinking = false,
            IsStopping = stopping,
        },
    };
}
