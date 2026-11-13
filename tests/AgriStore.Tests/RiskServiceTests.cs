using AgriStore.Web.Models;
using AgriStore.Web.Services;

namespace AgriStore.Tests;

public class RiskServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 12, 0, 0);
    private readonly RiskService _service = new();

    // Potato: 4-8 °C, 85-95 % humidity, 150 days.
    private static CropProfile Potato() => new()
    {
        Name = "Potato", MinTempC = 4, MaxTempC = 8, MinHumidity = 85, MaxHumidity = 95, ShelfLifeDays = 150
    };

    private static StockLot LotStoredDaysAgo(int days) => new() { StoredOn = Now.Date.AddDays(-days), QuantityTonnes = 10 };

    private static SensorReading Reading(double temp, double humidity, int hoursAgo = 1) =>
        new() { TemperatureC = temp, HumidityPercent = humidity, RecordedAt = Now.AddHours(-hoursAgo) };

    [Fact]
    public void FreshLot_InIdealConditions_IsLowRisk()
    {
        var result = _service.Assess(LotStoredDaysAgo(5), Potato(), Reading(6, 90), Now);

        Assert.Equal(RiskLevel.Low, result.Level);
        Assert.Equal(145, result.DaysLeft);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public void LotPastShelfLife_IsAtLeastHighRisk()
    {
        var result = _service.Assess(LotStoredDaysAgo(160), Potato(), Reading(6, 90), Now);

        Assert.True(result.Level >= RiskLevel.High);
        Assert.Equal(-10, result.DaysLeft);
        Assert.Contains(result.Reasons, r => r.Contains("Past shelf life"));
    }

    [Fact]
    public void HotStorage_RaisesRiskAndExplainsWhy()
    {
        var ideal = _service.Assess(LotStoredDaysAgo(5), Potato(), Reading(6, 90), Now);
        var hot = _service.Assess(LotStoredDaysAgo(5), Potato(), Reading(12, 90), Now);

        Assert.True(hot.Score > ideal.Score);
        Assert.Contains(hot.Reasons, r => r.Contains("above the safe range"));
    }

    [Fact]
    public void DryStorage_IsFlaggedAsBelowRange()
    {
        var result = _service.Assess(LotStoredDaysAgo(5), Potato(), Reading(6, 70), Now);

        Assert.Contains(result.Reasons, r => r.StartsWith("Humidity") && r.Contains("below"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(30)]
    public void MissingOrStaleReading_IsFlagged(int? hoursAgo)
    {
        var reading = hoursAgo is null ? null : Reading(6, 90, hoursAgo.Value);

        var result = _service.Assess(LotStoredDaysAgo(5), Potato(), reading, Now);

        Assert.Contains(result.Reasons, r => r.Contains("No sensor reading"));
    }

    [Fact]
    public void OldLotInBadConditions_IsCritical_AndScoreCapsAt100()
    {
        var result = _service.Assess(LotStoredDaysAgo(200), Potato(), Reading(30, 40), Now);

        Assert.Equal(RiskLevel.Critical, result.Level);
        Assert.Equal(100, result.Score);
        Assert.Equal("Sell, process or move this lot today.", result.Recommendation);
    }
}
