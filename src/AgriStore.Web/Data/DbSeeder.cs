using AgriStore.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Data;

/// <summary>Applies migrations and fills an empty database with demo data.</summary>
public static class DbSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await SeedAdminAsync(services);

        if (await db.Crops.AnyAsync()) return;

        // Ideal storage ranges (approximate values from post-harvest handling guides).
        var crops = new List<CropProfile>
        {
            new() { Name = "Rice",   MinTempC = 10, MaxTempC = 25, MinHumidity = 50, MaxHumidity = 65, ShelfLifeDays = 365 },
            new() { Name = "Wheat",  MinTempC = 10, MaxTempC = 25, MinHumidity = 45, MaxHumidity = 60, ShelfLifeDays = 300 },
            new() { Name = "Maize",  MinTempC = 10, MaxTempC = 25, MinHumidity = 50, MaxHumidity = 65, ShelfLifeDays = 240 },
            new() { Name = "Potato", MinTempC = 4,  MaxTempC = 8,  MinHumidity = 85, MaxHumidity = 95, ShelfLifeDays = 150 },
            new() { Name = "Onion",  MinTempC = 0,  MaxTempC = 5,  MinHumidity = 65, MaxHumidity = 70, ShelfLifeDays = 120 },
            new() { Name = "Tomato", MinTempC = 10, MaxTempC = 13, MinHumidity = 85, MaxHumidity = 95, ShelfLifeDays = 14 },
            new() { Name = "Apple",  MinTempC = 0,  MaxTempC = 4,  MinHumidity = 90, MaxHumidity = 95, ShelfLifeDays = 180 },
            new() { Name = "Banana", MinTempC = 13, MaxTempC = 15, MinHumidity = 85, MaxHumidity = 95, ShelfLifeDays = 21 },
        };
        db.Crops.AddRange(crops);

        var units = new List<StorageUnit>
        {
            new() { Name = "Guwahati Cold Store", Location = "Guwahati, Assam", Type = StorageType.ColdStorage, CapacityTonnes = 500 },
            new() { Name = "Ludhiana Grain Silo", Location = "Ludhiana, Punjab", Type = StorageType.Silo, CapacityTonnes = 2000 },
            new() { Name = "Nashik Onion Godown", Location = "Nashik, Maharashtra", Type = StorageType.Godown, CapacityTonnes = 800 },
            new() { Name = "Shillong Fruit Store", Location = "Shillong, Meghalaya", Type = StorageType.ColdStorage, CapacityTonnes = 300 },
        };
        db.StorageUnits.AddRange(units);

        CropProfile C(string name) => crops.First(c => c.Name == name);
        var today = DateTime.Today;
        db.StockLots.AddRange(
            new StockLot { Crop = C("Potato"), StorageUnit = units[0], FarmerName = "Ranjit Bora",     QuantityTonnes = 40,  StoredOn = today.AddDays(-30) },
            new StockLot { Crop = C("Tomato"), StorageUnit = units[0], FarmerName = "Mousumi Das",     QuantityTonnes = 6,   StoredOn = today.AddDays(-11) },
            new StockLot { Crop = C("Banana"), StorageUnit = units[0], FarmerName = "Hemanta Kalita",  QuantityTonnes = 9,   StoredOn = today.AddDays(-4) },
            new StockLot { Crop = C("Wheat"),  StorageUnit = units[1], FarmerName = "Gurpreet Singh",  QuantityTonnes = 420, StoredOn = today.AddDays(-60) },
            new StockLot { Crop = C("Rice"),   StorageUnit = units[1], FarmerName = "Harjit Kaur",     QuantityTonnes = 310, StoredOn = today.AddDays(-120) },
            new StockLot { Crop = C("Maize"),  StorageUnit = units[1], FarmerName = "Balwinder Sandhu",QuantityTonnes = 150, StoredOn = today.AddDays(-230) },
            new StockLot { Crop = C("Onion"),  StorageUnit = units[2], FarmerName = "Sunil Pawar",     QuantityTonnes = 220, StoredOn = today.AddDays(-45) },
            new StockLot { Crop = C("Onion"),  StorageUnit = units[2], FarmerName = "Anita Jadhav",    QuantityTonnes = 95,  StoredOn = today.AddDays(-100) },
            new StockLot { Crop = C("Apple"),  StorageUnit = units[3], FarmerName = "Bah Lyngdoh",     QuantityTonnes = 35,  StoredOn = today.AddDays(-20) },
            new StockLot { Crop = C("Potato"), StorageUnit = units[3], FarmerName = "Kong Syiem",      QuantityTonnes = 18,  StoredOn = today.AddDays(-5) },
            new StockLot { Crop = C("Rice"),   StorageUnit = units[1], FarmerName = "Jaspal Gill",     QuantityTonnes = 80,  StoredOn = today.AddDays(-200), Status = LotStatus.Dispatched });

        // 7 days of readings, every 6 hours. Two units drift out of range on purpose,
        // so the dashboard has real warnings to show.
        var random = new Random(42);
        var baseline = new (double Temp, double Humidity, double Drift)[]
        {
            (6, 88, 0.0),   // cold store: fine
            (22, 58, 0.15), // silo: warming up and getting damp
            (14, 74, 0.25), // onion godown: far too warm
            (3, 91, 0.0),   // fruit store: fine
        };
        for (var u = 0; u < units.Count; u++)
        {
            for (var h = 7 * 24; h >= 0; h -= 6)
            {
                var step = (7 * 24 - h) / 6;
                db.Readings.Add(new SensorReading
                {
                    StorageUnit = units[u],
                    RecordedAt = DateTime.Now.AddHours(-h),
                    TemperatureC = Math.Round(baseline[u].Temp + baseline[u].Drift * step + random.NextDouble() - 0.5, 1),
                    HumidityPercent = Math.Round(baseline[u].Humidity + baseline[u].Drift * step / 2 + random.NextDouble() * 2 - 1, 1),
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        if (!await roles.RoleExistsAsync(AdminRole))
            await roles.CreateAsync(new IdentityRole(AdminRole));

        var email = config["Seed:AdminEmail"] ?? "admin@agristore.local";
        var password = config["Seed:AdminPassword"] ?? "Admin@123";
        if (await users.FindByEmailAsync(email) is not null) return;

        var admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(admin, password);
        if (result.Succeeded)
            await users.AddToRoleAsync(admin, AdminRole);
    }
}
