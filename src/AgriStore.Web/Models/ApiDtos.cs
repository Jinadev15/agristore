using System.ComponentModel.DataAnnotations;

namespace AgriStore.Web.Models;

/// <summary>What a sensor device sends to POST /api/readings.</summary>
public record ReadingInput(
    [Range(1, int.MaxValue)] int StorageUnitId,
    [Range(-40, 80)] double TemperatureC,
    [Range(0, 100)] double HumidityPercent,
    DateTime? RecordedAt);

public record ReadingDto(int Id, int StorageUnitId, double TemperatureC, double HumidityPercent, DateTime RecordedAt);

public record UnitDto(int Id, string Name, string Location, string Type, double CapacityTonnes, double StoredTonnes, ReadingDto? LatestReading);

public record LotRiskDto(int LotId, string Crop, string Farmer, string StorageUnit, double QuantityTonnes,
    int RiskScore, string RiskLevel, int DaysLeft, List<string> Reasons, string Recommendation);
