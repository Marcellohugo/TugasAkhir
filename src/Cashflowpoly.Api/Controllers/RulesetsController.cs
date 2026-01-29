using Cashflowpoly.Api.Models;
using Cashflowpoly.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cashflowpoly.Api.Controllers;

[ApiController]
[Route("api/rulesets")]
public sealed class RulesetsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateRuleset([FromBody] CreateRulesetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Field wajib tidak lengkap",
                new ErrorDetail("name", "REQUIRED")));
        }

        var rulesetId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var ruleset = new RulesetRecord(
            rulesetId,
            request.Name,
            request.Description,
            false,
            createdAt,
            null);

        var rulesetVersionId = Guid.NewGuid();
        var version = new RulesetVersionRecord(
            rulesetVersionId,
            rulesetId,
            1,
            "ACTIVE",
            request.Config.GetRawText(),
            createdAt,
            null);

        InMemoryStore.Rulesets[rulesetId] = ruleset;
        InMemoryStore.RulesetVersions[rulesetVersionId] = version;

        return Created($"/api/rulesets/{rulesetId}", new CreateRulesetResponse(rulesetId, 1));
    }

    [HttpPut("{rulesetId:guid}")]
    public IActionResult UpdateRuleset(Guid rulesetId, [FromBody] UpdateRulesetRequest request)
    {
        if (!InMemoryStore.Rulesets.TryGetValue(rulesetId, out var ruleset))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Ruleset tidak ditemukan"));
        }

        if (request.Config is null)
        {
            return BadRequest(ApiErrorHelper.BuildError(HttpContext, "VALIDATION_ERROR", "Config wajib ada",
                new ErrorDetail("config", "REQUIRED")));
        }

        var latestVersion = InMemoryStore.RulesetVersions.Values
            .Where(v => v.RulesetId == rulesetId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        var nextVersion = (latestVersion?.Version ?? 0) + 1;
        var createdAt = DateTimeOffset.UtcNow;

        var updatedRuleset = ruleset with
        {
            Name = request.Name ?? ruleset.Name,
            Description = request.Description ?? ruleset.Description
        };

        var rulesetVersionId = Guid.NewGuid();
        var version = new RulesetVersionRecord(
            rulesetVersionId,
            rulesetId,
            nextVersion,
            "ACTIVE",
            request.Config.Value.GetRawText(),
            createdAt,
            null);

        InMemoryStore.Rulesets[rulesetId] = updatedRuleset;
        InMemoryStore.RulesetVersions[rulesetVersionId] = version;

        return Ok(new CreateRulesetResponse(rulesetId, nextVersion));
    }

    [HttpGet]
    public IActionResult ListRulesets()
    {
        var items = InMemoryStore.Rulesets.Values
            .OrderByDescending(r => r.CreatedAt)
            .Select(r =>
            {
                var latestVersion = InMemoryStore.RulesetVersions.Values
                    .Where(v => v.RulesetId == r.RulesetId)
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault();

                return new RulesetListItem(r.RulesetId, r.Name, latestVersion?.Version ?? 0);
            })
            .ToList();

        return Ok(new RulesetListResponse(items));
    }
}
