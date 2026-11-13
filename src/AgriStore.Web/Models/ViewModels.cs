using AgriStore.Web.Services;

namespace AgriStore.Web.Models;

public record ErrorViewModel(string? RequestId);

public record LotWithRisk(StockLot Lot, RiskAssessment Risk);

/// <summary>A storage unit with its latest reading and whether that reading suits every crop inside.</summary>
public record UnitStatus(StorageUnit Unit, SensorReading? Latest, double StoredTonnes, int LotsOutOfRange)
{
    public double FillPercent => Unit.CapacityTonnes == 0 ? 0 : Math.Round(StoredTonnes / Unit.CapacityTonnes * 100, 1);
}

public class DashboardViewModel
{
    public double TotalStoredTonnes { get; init; }
    public int ActiveLots { get; init; }
    public int LotsAtRisk { get; init; }
    public double TonnesAtRisk { get; init; }
    public List<LotWithRisk> TopRisks { get; init; } = [];
    public List<UnitStatus> Units { get; init; } = [];
}
