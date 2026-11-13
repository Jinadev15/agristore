using AgriStore.Web.Data;
using AgriStore.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Services;

/// <summary>Loads stock and sensor data and combines them with the risk engine. Used by the dashboard, pages and API.</summary>
public class InsightService(ApplicationDbContext db, IRiskService risk, TimeProvider clock)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    /// <summary>All lots still in storage, each with its risk, worst first.</summary>
    public async Task<List<LotWithRisk>> GetStoredLotsWithRiskAsync(int? storageUnitId = null)
    {
        var lots = await db.StockLots
            .Include(l => l.Crop)
            .Include(l => l.StorageUnit)
            .Where(l => l.Status == LotStatus.Stored)
            .Where(l => storageUnitId == null || l.StorageUnitId == storageUnitId)
            .AsNoTracking()
            .ToListAsync();

        var latest = await GetLatestReadingsAsync();

        return lots
            .Select(l => new LotWithRisk(l, risk.Assess(l, l.Crop!, latest.GetValueOrDefault(l.StorageUnitId), Now)))
            .OrderByDescending(x => x.Risk.Score)
            .ToList();
    }

    public async Task<LotWithRisk?> GetLotWithRiskAsync(int lotId)
    {
        var lot = await db.StockLots.Include(l => l.Crop).Include(l => l.StorageUnit)
            .AsNoTracking().FirstOrDefaultAsync(l => l.Id == lotId);
        if (lot is null) return null;

        var latest = await db.Readings.Where(r => r.StorageUnitId == lot.StorageUnitId)
            .OrderByDescending(r => r.RecordedAt).FirstOrDefaultAsync();
        return new LotWithRisk(lot, risk.Assess(lot, lot.Crop!, latest, Now));
    }

    public async Task<List<UnitStatus>> GetUnitStatusesAsync()
    {
        var units = await db.StorageUnits.AsNoTracking().OrderBy(u => u.Name).ToListAsync();
        var latest = await GetLatestReadingsAsync();
        var lots = await GetStoredLotsWithRiskAsync();

        return units.Select(u =>
        {
            var unitLots = lots.Where(x => x.Lot.StorageUnitId == u.Id).ToList();
            var outOfRange = unitLots.Count(x => x.Risk.Reasons.Any(r => r.Contains("safe range")));
            return new UnitStatus(u, latest.GetValueOrDefault(u.Id), unitLots.Sum(x => x.Lot.QuantityTonnes), outOfRange);
        }).ToList();
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var lots = await GetStoredLotsWithRiskAsync();
        var atRisk = lots.Where(x => x.Risk.Level >= RiskLevel.High).ToList();

        return new DashboardViewModel
        {
            TotalStoredTonnes = lots.Sum(x => x.Lot.QuantityTonnes),
            ActiveLots = lots.Count,
            LotsAtRisk = atRisk.Count,
            TonnesAtRisk = atRisk.Sum(x => x.Lot.QuantityTonnes),
            TopRisks = lots.Where(x => x.Risk.Level >= RiskLevel.Medium).Take(6).ToList(),
            Units = await GetUnitStatusesAsync()
        };
    }

    /// <summary>Latest reading per storage unit, keyed by unit id.</summary>
    private async Task<Dictionary<int, SensorReading>> GetLatestReadingsAsync()
    {
        var latestTimes = db.Readings
            .GroupBy(r => r.StorageUnitId)
            .Select(g => new { UnitId = g.Key, At = g.Max(r => r.RecordedAt) });

        var readings = await db.Readings.AsNoTracking()
            .Where(r => latestTimes.Any(t => t.UnitId == r.StorageUnitId && t.At == r.RecordedAt))
            .ToListAsync();

        return readings.GroupBy(r => r.StorageUnitId).ToDictionary(g => g.Key, g => g.First());
    }
}
