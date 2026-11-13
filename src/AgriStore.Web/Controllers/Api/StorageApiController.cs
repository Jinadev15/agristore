using AgriStore.Web.Data;
using AgriStore.Web.Models;
using AgriStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers.Api;

/// <summary>
/// REST API for sensor devices and other apps. Reads are open; writes need the X-Api-Key header
/// (checked by ApiKeyMiddleware).
/// </summary>
[ApiController]
[Route("api")]
[AllowAnonymous]
[Produces("application/json")]
public class StorageApiController(ApplicationDbContext db, InsightService insights) : ControllerBase
{
    /// <summary>All storage units with stored tonnes and the latest reading.</summary>
    [HttpGet("units")]
    public async Task<ActionResult<List<UnitDto>>> GetUnits()
    {
        var statuses = await insights.GetUnitStatusesAsync();
        return statuses.Select(s => new UnitDto(s.Unit.Id, s.Unit.Name, s.Unit.Location, s.Unit.Type.ToString(),
            s.Unit.CapacityTonnes, s.StoredTonnes, s.Latest is null ? null : ToDto(s.Latest))).ToList();
    }

    /// <summary>Readings for one unit, newest first.</summary>
    [HttpGet("units/{id:int}/readings")]
    public async Task<ActionResult<List<ReadingDto>>> GetReadings(int id, int take = 50)
    {
        if (!await db.StorageUnits.AnyAsync(u => u.Id == id)) return NotFound();

        return await db.Readings.Where(r => r.StorageUnitId == id)
            .OrderByDescending(r => r.RecordedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(r => new ReadingDto(r.Id, r.StorageUnitId, r.TemperatureC, r.HumidityPercent, r.RecordedAt))
            .ToListAsync();
    }

    /// <summary>Stored lots at or above a risk level (default Medium), worst first.</summary>
    [HttpGet("lots/at-risk")]
    public async Task<ActionResult<List<LotRiskDto>>> GetLotsAtRisk(RiskLevel minLevel = RiskLevel.Medium)
    {
        var lots = await insights.GetStoredLotsWithRiskAsync();
        return lots.Where(x => x.Risk.Level >= minLevel)
            .Select(x => new LotRiskDto(x.Lot.Id, x.Lot.Crop!.Name, x.Lot.FarmerName, x.Lot.StorageUnit!.Name,
                x.Lot.QuantityTonnes, x.Risk.Score, x.Risk.Level.ToString(), x.Risk.DaysLeft, x.Risk.Reasons,
                x.Risk.Recommendation))
            .ToList();
    }

    /// <summary>A sensor device posts a temperature/humidity reading. Needs the X-Api-Key header.</summary>
    [HttpPost("readings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReadingDto>> PostReading(ReadingInput input)
    {
        if (!await db.StorageUnits.AnyAsync(u => u.Id == input.StorageUnitId))
            return ValidationProblem($"Storage unit {input.StorageUnitId} does not exist.");

        var reading = new SensorReading
        {
            StorageUnitId = input.StorageUnitId,
            TemperatureC = input.TemperatureC,
            HumidityPercent = input.HumidityPercent,
            RecordedAt = input.RecordedAt ?? DateTime.Now
        };
        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReadings), new { id = reading.StorageUnitId }, ToDto(reading));
    }

    private static ReadingDto ToDto(SensorReading r) =>
        new(r.Id, r.StorageUnitId, r.TemperatureC, r.HumidityPercent, r.RecordedAt);
}
