using AgriStore.Web.Models;

namespace AgriStore.Web.Services;

public enum RiskLevel { Low, Medium, High, Critical }

/// <summary>Result of checking one stock lot: a 0-100 score, its level, why, and what to do.</summary>
public record RiskAssessment(int Score, RiskLevel Level, int DaysLeft, List<string> Reasons, string Recommendation);

public interface IRiskService
{
    RiskAssessment Assess(StockLot lot, CropProfile crop, SensorReading? latestReading, DateTime now);
}

/// <summary>
/// Scores spoilage risk from three things: how old the produce is compared with its shelf life,
/// how far storage conditions are from the crop's ideal range, and whether sensor data is stale.
/// </summary>
public class RiskService : IRiskService
{
    public const int StaleAfterHours = 24;

    public RiskAssessment Assess(StockLot lot, CropProfile crop, SensorReading? latestReading, DateTime now)
    {
        var reasons = new List<string>();
        double score = 0;

        // 1. Age: up to 50 points as the lot nears the end of its shelf life, 60 once past it.
        var daysStored = (now.Date - lot.StoredOn.Date).Days;
        var daysLeft = crop.ShelfLifeDays - daysStored;
        var ageRatio = (double)daysStored / crop.ShelfLifeDays;
        if (daysLeft <= 0)
        {
            score += 60;
            reasons.Add($"Past shelf life by {Days(-daysLeft)}");
        }
        else
        {
            score += Math.Clamp(ageRatio, 0, 1) * 50;
            if (ageRatio >= 0.75) reasons.Add($"Only {Days(daysLeft)} of shelf life left");
        }

        // 2. Storage conditions, from the latest sensor reading.
        if (latestReading is null || now - latestReading.RecordedAt > TimeSpan.FromHours(StaleAfterHours))
        {
            score += 10;
            reasons.Add("No sensor reading in the last 24 hours");
        }
        if (latestReading is not null)
        {
            var tempOff = DistanceOutside(latestReading.TemperatureC, crop.MinTempC, crop.MaxTempC);
            if (tempOff > 0)
            {
                score += Math.Min(tempOff * 8, 30);
                var direction = latestReading.TemperatureC > crop.MaxTempC ? "above" : "below";
                reasons.Add($"Temperature {latestReading.TemperatureC:0.#}°C is {tempOff:0.#}°C {direction} the safe range");
            }

            var humidityOff = DistanceOutside(latestReading.HumidityPercent, crop.MinHumidity, crop.MaxHumidity);
            if (humidityOff > 0)
            {
                score += Math.Min(humidityOff * 1.5, 20);
                var direction = latestReading.HumidityPercent > crop.MaxHumidity ? "above" : "below";
                reasons.Add($"Humidity {latestReading.HumidityPercent:0.#}% is {humidityOff:0.#}% {direction} the safe range");
            }
        }

        var finalScore = (int)Math.Round(Math.Min(score, 100));
        var level = finalScore switch
        {
            >= 80 => RiskLevel.Critical,
            >= 55 => RiskLevel.High,
            >= 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        return new RiskAssessment(finalScore, level, daysLeft, reasons, Recommend(level));
    }

    private static string Days(int n) => n == 1 ? "1 day" : $"{n} days";

    private static double DistanceOutside(double value, double min, double max) =>
        value < min ? min - value : value > max ? value - max : 0;

    private static string Recommend(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "Sell, process or move this lot today.",
        RiskLevel.High => "Plan dispatch within a few days and fix storage conditions now.",
        RiskLevel.Medium => "Check conditions daily and line up a buyer.",
        _ => "Storage conditions are good. No action needed."
    };
}
