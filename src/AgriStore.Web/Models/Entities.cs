using System.ComponentModel.DataAnnotations;

namespace AgriStore.Web.Models;

public enum StorageType
{
    [Display(Name = "Cold Storage")] ColdStorage,
    Warehouse,
    Silo,
    Godown
}

public enum LotStatus
{
    Stored,
    Dispatched,
    Spoiled
}

/// <summary>A crop and the storage conditions it needs to stay fresh.</summary>
public class CropProfile
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = "";

    [Display(Name = "Min temp (°C)")]
    public double MinTempC { get; set; }

    [Display(Name = "Max temp (°C)")]
    public double MaxTempC { get; set; }

    [Display(Name = "Min humidity (%)"), Range(0, 100)]
    public double MinHumidity { get; set; }

    [Display(Name = "Max humidity (%)"), Range(0, 100)]
    public double MaxHumidity { get; set; }

    [Display(Name = "Shelf life (days)"), Range(1, 3650)]
    public int ShelfLifeDays { get; set; }

    public List<StockLot> Lots { get; set; } = [];
}

/// <summary>A physical place where produce is kept: cold store, warehouse, silo or godown.</summary>
public class StorageUnit
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";

    [Required, StringLength(120)]
    public string Location { get; set; } = "";

    public StorageType Type { get; set; }

    [Display(Name = "Capacity (tonnes)"), Range(1, 100000)]
    public double CapacityTonnes { get; set; }

    public List<StockLot> Lots { get; set; } = [];
    public List<SensorReading> Readings { get; set; } = [];
}

/// <summary>One batch of a farmer's produce kept in a storage unit.</summary>
public class StockLot
{
    public int Id { get; set; }

    [Display(Name = "Crop")]
    public int CropProfileId { get; set; }
    public CropProfile? Crop { get; set; }

    [Display(Name = "Storage unit")]
    public int StorageUnitId { get; set; }
    public StorageUnit? StorageUnit { get; set; }

    [Required, StringLength(80), Display(Name = "Farmer name")]
    public string FarmerName { get; set; } = "";

    [Display(Name = "Quantity (tonnes)"), Range(0.01, 10000)]
    public double QuantityTonnes { get; set; }

    [Display(Name = "Stored on"), DataType(DataType.Date)]
    public DateTime StoredOn { get; set; } = DateTime.Today;

    public LotStatus Status { get; set; } = LotStatus.Stored;
}

/// <summary>A temperature and humidity reading from a storage unit's sensor.</summary>
public class SensorReading
{
    public int Id { get; set; }

    [Display(Name = "Storage unit")]
    public int StorageUnitId { get; set; }
    public StorageUnit? StorageUnit { get; set; }

    [Display(Name = "Temperature (°C)"), Range(-40, 80)]
    public double TemperatureC { get; set; }

    [Display(Name = "Humidity (%)"), Range(0, 100)]
    public double HumidityPercent { get; set; }

    [Display(Name = "Recorded at")]
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}

public static class StorageTypeExtensions
{
    /// <summary>Readable name for screens, e.g. "Cold storage" instead of "ColdStorage".</summary>
    public static string Label(this StorageType type) => type == StorageType.ColdStorage ? "Cold storage" : type.ToString();
}
