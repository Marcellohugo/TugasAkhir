using System.Text.Json;
using Cashflowpoly.Api.Models;
using Cashflowpoly.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cashflowpoly.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    [HttpGet("sessions/{sessionId:guid}")]
    public IActionResult GetSessionAnalytics(Guid sessionId)
    {
        if (!InMemoryStore.Sessions.ContainsKey(sessionId))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        var events = InMemoryStore.EventsBySession.TryGetValue(sessionId, out var list)
            ? list
            : new List<EventRecord>();

        var summary = BuildSummary(events);
        var byPlayer = BuildByPlayer(events);

        return Ok(new AnalyticsSessionResponse(sessionId, summary, byPlayer));
    }

    [HttpGet("sessions/{sessionId:guid}/transactions")]
    public IActionResult GetTransactions(Guid sessionId, [FromQuery] Guid? playerId = null)
    {
        if (!InMemoryStore.Sessions.ContainsKey(sessionId))
        {
            return NotFound(ApiErrorHelper.BuildError(HttpContext, "NOT_FOUND", "Session tidak ditemukan"));
        }

        var events = InMemoryStore.EventsBySession.TryGetValue(sessionId, out var list)
            ? list
            : new List<EventRecord>();

        var items = new List<TransactionHistoryItem>();

        foreach (var evt in events.Where(e => e.ActionType == "transaction.recorded"))
        {
            if (playerId.HasValue && evt.PlayerId != playerId.Value)
            {
                continue;
            }

            if (!TryReadTransaction(evt.PayloadJson, out var direction, out var amount, out var category))
            {
                continue;
            }

            items.Add(new TransactionHistoryItem(evt.Timestamp, direction, amount, category));
        }

        return Ok(new TransactionHistoryResponse(items));
    }

    private static AnalyticsSessionSummary BuildSummary(List<EventRecord> events)
    {
        var cashInTotal = 0d;
        var cashOutTotal = 0d;

        foreach (var evt in events.Where(e => e.ActionType == "transaction.recorded"))
        {
            if (!TryReadTransaction(evt.PayloadJson, out var direction, out var amount, out _))
            {
                continue;
            }

            if (string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase))
            {
                cashInTotal += amount;
            }
            else if (string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase))
            {
                cashOutTotal += amount;
            }
        }

        return new AnalyticsSessionSummary(events.Count, cashInTotal, cashOutTotal);
    }

    private static List<AnalyticsByPlayerItem> BuildByPlayer(List<EventRecord> events)
    {
        var byPlayer = new Dictionary<Guid, AnalyticsByPlayerItem>();

        foreach (var evt in events)
        {
            if (evt.PlayerId is null)
            {
                continue;
            }

            var playerId = evt.PlayerId.Value;
            if (!byPlayer.TryGetValue(playerId, out var item))
            {
                item = new AnalyticsByPlayerItem(playerId, 0, 0, 0, 0);
            }

            if (evt.ActionType == "transaction.recorded" &&
                TryReadTransaction(evt.PayloadJson, out var direction, out var amount, out _))
            {
                if (string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase))
                {
                    item = item with { CashInTotal = item.CashInTotal + amount };
                }
                else if (string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase))
                {
                    item = item with { CashOutTotal = item.CashOutTotal + amount };
                }
            }

            if (evt.ActionType == "day.friday.donation" && TryReadAmount(evt.PayloadJson, out var donationAmount))
            {
                item = item with { DonationTotal = item.DonationTotal + donationAmount };
            }

            if (evt.ActionType == "day.saturday.gold_trade" &&
                TryReadGoldTrade(evt.PayloadJson, out var tradeType, out var qty))
            {
                item = item with
                {
                    GoldQty = item.GoldQty + (string.Equals(tradeType, "BUY", StringComparison.OrdinalIgnoreCase) ? qty : -qty)
                };
            }

            byPlayer[playerId] = item;
        }

        return byPlayer.Values.ToList();
    }

    private static bool TryReadTransaction(string payloadJson, out string direction, out double amount, out string category)
    {
        direction = string.Empty;
        category = string.Empty;
        amount = 0;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("direction", out var directionProp) ||
                !root.TryGetProperty("amount", out var amountProp) ||
                !root.TryGetProperty("category", out var categoryProp))
            {
                return false;
            }

            direction = directionProp.GetString() ?? string.Empty;
            category = categoryProp.GetString() ?? string.Empty;
            amount = amountProp.GetDouble();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadAmount(string payloadJson, out double amount)
    {
        amount = 0;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("amount", out var amountProp))
            {
                return false;
            }

            amount = amountProp.GetDouble();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadGoldTrade(string payloadJson, out string tradeType, out int qty)
    {
        tradeType = string.Empty;
        qty = 0;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("trade_type", out var tradeTypeProp) ||
                !root.TryGetProperty("qty", out var qtyProp))
            {
                return false;
            }

            tradeType = tradeTypeProp.GetString() ?? string.Empty;
            qty = qtyProp.GetInt32();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
