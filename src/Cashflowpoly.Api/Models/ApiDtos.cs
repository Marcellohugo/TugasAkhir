using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cashflowpoly.Api.Models;

public sealed record CreateSessionRequest(
    [property: JsonPropertyName("session_name")] string SessionName,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("ruleset_id")] Guid RulesetId);

public sealed record CreateSessionResponse([property: JsonPropertyName("session_id")] Guid SessionId);

public sealed record SessionStatusResponse([property: JsonPropertyName("status")] string Status);

public sealed record ActivateRulesetRequest(
    [property: JsonPropertyName("ruleset_id")] Guid RulesetId,
    [property: JsonPropertyName("version")] int Version);

public sealed record ActivateRulesetResponse(
    [property: JsonPropertyName("session_id")] Guid SessionId,
    [property: JsonPropertyName("ruleset_version_id")] Guid RulesetVersionId);

public sealed record CreateRulesetRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("config")] JsonElement Config);

public sealed record UpdateRulesetRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("config")] JsonElement? Config);

public sealed record CreateRulesetResponse(
    [property: JsonPropertyName("ruleset_id")] Guid RulesetId,
    [property: JsonPropertyName("version")] int Version);

public sealed record RulesetListItem(
    [property: JsonPropertyName("ruleset_id")] Guid RulesetId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("latest_version")] int LatestVersion);

public sealed record RulesetListResponse([property: JsonPropertyName("items")] List<RulesetListItem> Items);

public sealed record EventRequest(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("session_id")] Guid SessionId,
    [property: JsonPropertyName("player_id")] Guid? PlayerId,
    [property: JsonPropertyName("actor_type")] string ActorType,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("day_index")] int DayIndex,
    [property: JsonPropertyName("weekday")] string Weekday,
    [property: JsonPropertyName("turn_number")] int TurnNumber,
    [property: JsonPropertyName("sequence_number")] long SequenceNumber,
    [property: JsonPropertyName("action_type")] string ActionType,
    [property: JsonPropertyName("ruleset_version_id")] Guid RulesetVersionId,
    [property: JsonPropertyName("payload")] JsonElement Payload,
    [property: JsonPropertyName("client_request_id")] string? ClientRequestId);

public sealed record EventStoredResponse(
    [property: JsonPropertyName("stored")] bool Stored,
    [property: JsonPropertyName("event_id")] Guid EventId);

public sealed record EventBatchRequest([property: JsonPropertyName("events")] List<EventRequest> Events);

public sealed record EventBatchFailed(
    [property: JsonPropertyName("event_id")] Guid EventId,
    [property: JsonPropertyName("error_code")] string ErrorCode);

public sealed record EventBatchResponse(
    [property: JsonPropertyName("stored_count")] int StoredCount,
    [property: JsonPropertyName("failed")] List<EventBatchFailed> Failed);

public sealed record EventsBySessionResponse(
    [property: JsonPropertyName("session_id")] Guid SessionId,
    [property: JsonPropertyName("events")] List<EventRequest> Events);

public sealed record AnalyticsSessionSummary(
    [property: JsonPropertyName("event_count")] int EventCount,
    [property: JsonPropertyName("cash_in_total")] double CashInTotal,
    [property: JsonPropertyName("cash_out_total")] double CashOutTotal);

public sealed record AnalyticsByPlayerItem(
    [property: JsonPropertyName("player_id")] Guid PlayerId,
    [property: JsonPropertyName("cash_in_total")] double CashInTotal,
    [property: JsonPropertyName("cash_out_total")] double CashOutTotal,
    [property: JsonPropertyName("donation_total")] double DonationTotal,
    [property: JsonPropertyName("gold_qty")] int GoldQty);

public sealed record AnalyticsSessionResponse(
    [property: JsonPropertyName("session_id")] Guid SessionId,
    [property: JsonPropertyName("summary")] AnalyticsSessionSummary Summary,
    [property: JsonPropertyName("by_player")] List<AnalyticsByPlayerItem> ByPlayer);

public sealed record TransactionHistoryItem(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("direction")] string Direction,
    [property: JsonPropertyName("amount")] double Amount,
    [property: JsonPropertyName("category")] string Category);

public sealed record TransactionHistoryResponse(
    [property: JsonPropertyName("items")] List<TransactionHistoryItem> Items);
