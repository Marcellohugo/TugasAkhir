using System.Text.Json;
using Cashflowpoly.Api.Models;
using Cashflowpoly.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cashflowpoly.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class EventsController : ControllerBase
{
    private static readonly HashSet<string> AllowedActorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLAYER",
        "SYSTEM"
    };

    private static readonly HashSet<string> AllowedWeekdays = new(StringComparer.OrdinalIgnoreCase)
    {
        "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
    };

    [HttpPost("events")]
    public IActionResult CreateEvent([FromBody] EventRequest request)
    {
        if (!TryValidateEvent(request, out var errorResult))
        {
            return errorResult;
        }

        var stored = StoreEvent(request);
        if (!stored.IsSuccess)
        {
            return stored.Result;
        }

        return StatusCode(StatusCodes.Status201Created, new EventStoredResponse(true, request.EventId));
    }

    [HttpPost("events/batch")]
    public IActionResult CreateEventsBatch([FromBody] EventBatchRequest request)
    {
        var failed = new List<EventBatchFailed>();
        var storedCount = 0;

        foreach (var evt in request.Events)
        {
            if (!TryValidateEvent(evt, out var errorResult))
            {
                failed.Add(new EventBatchFailed(evt.EventId, "VALIDATION_ERROR"));
                continue;
            }

            var stored = StoreEvent(evt);
            if (!stored.IsSuccess)
            {
                failed.Add(new EventBatchFailed(evt.EventId, stored.ErrorCode));
                continue;
            }

            storedCount++;
        }

        return Ok(new EventBatchResponse(storedCount, failed));
    }

    [HttpGet("sessions/{sessionId:guid}/events")]
    public IActionResult GetEventsBySession(Guid sessionId, [FromQuery] long fromSeq = 0, [FromQuery] int limit = 200)
    {
        if (!InMemoryStore.Sessions.ContainsKey(sessionId))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        var events = InMemoryStore.EventsBySession.TryGetValue(sessionId, out var list)
            ? list.OrderBy(e => e.SequenceNumber).SkipWhile(e => e.SequenceNumber < fromSeq).Take(limit).ToList()
            : new List<EventRecord>();

        var responseEvents = events.Select(ToEventRequest).ToList();
        return Ok(new EventsBySessionResponse(sessionId, responseEvents));
    }

    private bool TryValidateEvent(EventRequest request, out IActionResult errorResult)
    {
        if (!InMemoryStore.Sessions.TryGetValue(request.SessionId, out var session))
        {
            errorResult = NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
            return false;
        }

        if (!InMemoryStore.RulesetVersions.ContainsKey(request.RulesetVersionId))
        {
            errorResult = NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Ruleset version tidak ditemukan"));
            return false;
        }

        if (session.ActiveRulesetVersionId != request.RulesetVersionId)
        {
            errorResult = UnprocessableEntity(ApiErrorHelper.BuildError(HttpContext, "DOMAIN_RULE_VIOLATION", "Ruleset version tidak aktif"));
            return false;
        }

        if (!AllowedActorTypes.Contains(request.ActorType))
        {
            errorResult = BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Actor type tidak valid",
                new ErrorDetail("actor_type", "INVALID_ENUM")));
            return false;
        }

        if (!AllowedWeekdays.Contains(request.Weekday))
        {
            errorResult = BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Weekday tidak valid",
                new ErrorDetail("weekday", "INVALID_ENUM")));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ActionType))
        {
            errorResult = BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Action type wajib diisi",
                new ErrorDetail("action_type", "REQUIRED")));
            return false;
        }

        errorResult = null!;
        return true;
    }

    private (bool IsSuccess, IActionResult Result, string ErrorCode) StoreEvent(EventRequest request)
    {
        var events = InMemoryStore.EventsBySession.GetOrAdd(request.SessionId, _ => new List<EventRecord>());

        if (events.Any(e => e.EventId == request.EventId))
        {
            var result = Conflict(ApiErrorHelper.BuildError(HttpContext, "DUPLICATE", "Event sudah ada"));
            return (false, result, "DUPLICATE");
        }

        if (events.Any(e => e.SequenceNumber == request.SequenceNumber))
        {
            var result = Conflict(ApiErrorHelper.BuildError(HttpContext, "DUPLICATE", "Sequence number sudah ada"));
            return (false, result, "DUPLICATE");
        }

        var record = new EventRecord(
            Guid.NewGuid(),
            request.EventId,
            request.SessionId,
            request.PlayerId,
            request.ActorType.ToUpperInvariant(),
            request.Timestamp,
            request.DayIndex,
            request.Weekday.ToUpperInvariant(),
            request.TurnNumber,
            request.SequenceNumber,
            request.ActionType,
            request.RulesetVersionId,
            request.Payload.GetRawText(),
            DateTimeOffset.UtcNow,
            request.ClientRequestId);

        events.Add(record);

        return (true, Ok(), string.Empty);
    }

    private static EventRequest ToEventRequest(EventRecord record)
    {
        using var document = JsonDocument.Parse(record.PayloadJson);
        var payload = document.RootElement.Clone();

        return new EventRequest(
            record.EventId,
            record.SessionId,
            record.PlayerId,
            record.ActorType,
            record.Timestamp,
            record.DayIndex,
            record.Weekday,
            record.TurnNumber,
            record.SequenceNumber,
            record.ActionType,
            record.RulesetVersionId,
            payload,
            record.ClientRequestId);
    }
}
