using System.Collections.Concurrent;

namespace Cashflowpoly.Api.Storage;

internal static class InMemoryStore
{
    internal static ConcurrentDictionary<Guid, RulesetRecord> Rulesets { get; } = new();
    internal static ConcurrentDictionary<Guid, RulesetVersionRecord> RulesetVersions { get; } = new();
    internal static ConcurrentDictionary<Guid, SessionRecord> Sessions { get; } = new();
    internal static ConcurrentDictionary<Guid, List<EventRecord>> EventsBySession { get; } = new();
}

internal sealed record RulesetRecord(
    Guid RulesetId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

internal sealed record RulesetVersionRecord(
    Guid RulesetVersionId,
    Guid RulesetId,
    int Version,
    string Status,
    string ConfigJson,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

internal sealed record SessionRecord(
    Guid SessionId,
    string SessionName,
    string Mode,
    string Status,
    Guid ActiveRulesetVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt);

internal sealed record EventRecord(
    Guid EventPk,
    Guid EventId,
    Guid SessionId,
    Guid? PlayerId,
    string ActorType,
    DateTimeOffset Timestamp,
    int DayIndex,
    string Weekday,
    int TurnNumber,
    long SequenceNumber,
    string ActionType,
    Guid RulesetVersionId,
    string PayloadJson,
    DateTimeOffset ReceivedAt,
    string? ClientRequestId);
