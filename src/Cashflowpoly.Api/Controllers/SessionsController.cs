using Cashflowpoly.Api.Models;
using Cashflowpoly.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cashflowpoly.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateSession([FromBody] CreateSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionName))
        {
            return BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Field wajib tidak lengkap",
                new ErrorDetail("session_name", "REQUIRED")));
        }

        if (!string.Equals(request.Mode, "PEMULA", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Mode, "MAHIR", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Mode tidak valid",
                new ErrorDetail("mode", "INVALID_ENUM")));
        }

        if (!InMemoryStore.Rulesets.ContainsKey(request.RulesetId))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Ruleset tidak ditemukan"));
        }

        var latestVersion = InMemoryStore.RulesetVersions.Values
            .Where(v => v.RulesetId == request.RulesetId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (latestVersion is null)
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Ruleset belum memiliki versi"));
        }

        var sessionId = Guid.NewGuid();
        var session = new SessionRecord(
            sessionId,
            request.SessionName,
            request.Mode.ToUpperInvariant(),
            "CREATED",
            latestVersion.RulesetVersionId,
            DateTimeOffset.UtcNow,
            null,
            null);

        InMemoryStore.Sessions[sessionId] = session;

        return Created($"/api/sessions/{sessionId}", new CreateSessionResponse(sessionId));
    }

    [HttpPost("{sessionId:guid}/start")]
    public IActionResult StartSession(Guid sessionId)
    {
        if (!InMemoryStore.Sessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        if (!string.Equals(session.Status, "CREATED", StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(ApiErrorHelper.BuildError(HttpContext, "DOMAIN_RULE_VIOLATION", "Status sesi tidak valid"));
        }

        var updated = session with { Status = "STARTED", StartedAt = DateTimeOffset.UtcNow };
        InMemoryStore.Sessions[sessionId] = updated;

        return Ok(new SessionStatusResponse(updated.Status));
    }

    [HttpPost("{sessionId:guid}/end")]
    public IActionResult EndSession(Guid sessionId)
    {
        if (!InMemoryStore.Sessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        if (!string.Equals(session.Status, "STARTED", StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(ApiErrorHelper.BuildError(HttpContext, "DOMAIN_RULE_VIOLATION", "Status sesi tidak valid"));
        }

        var updated = session with { Status = "ENDED", EndedAt = DateTimeOffset.UtcNow };
        InMemoryStore.Sessions[sessionId] = updated;

        return Ok(new SessionStatusResponse(updated.Status));
    }

    [HttpPost("{sessionId:guid}/ruleset/activate")]
    public IActionResult ActivateRuleset(Guid sessionId, [FromBody] ActivateRulesetRequest request)
    {
        if (!InMemoryStore.Sessions.TryGetValue(sessionId, out var session))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        var rulesetVersion = InMemoryStore.RulesetVersions.Values.FirstOrDefault(v =>
            v.RulesetId == request.RulesetId && v.Version == request.Version);

        if (rulesetVersion is null)
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Ruleset version tidak ditemukan"));
        }

        var updated = session with { ActiveRulesetVersionId = rulesetVersion.RulesetVersionId };
        InMemoryStore.Sessions[sessionId] = updated;

        return Ok(new ActivateRulesetResponse(sessionId, rulesetVersion.RulesetVersionId));
    }
}
