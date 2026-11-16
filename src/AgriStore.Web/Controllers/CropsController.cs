using AgriStore.Web.Data;
using AgriStore.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers;

/// <summary>Crop profiles: the ideal storage range and shelf life the risk engine compares against.</summary>
public class CropsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Crops.OrderBy(c => c.Name).ToListAsync());

    [Authorize(Roles = DbSeeder.AdminRole)]
    public IActionResult Create() => View("Form", new CropProfile { ShelfLifeDays = 30 });

    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var crop = await db.Crops.FindAsync(id);
        return crop is null ? NotFound() : View("Form", crop);
    }

    [HttpGet, Authorize(Roles = DbSeeder.AdminRole)]
    public IActionResult Save(int? id) => id is > 0 ? RedirectToAction(nameof(Edit), new { id }) : RedirectToAction(nameof(Create));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Save(CropProfile crop)
    {
        crop.Name = crop.Name?.Trim() ?? "";
        if (crop.MinTempC > crop.MaxTempC)
            ModelState.AddModelError(nameof(crop.MaxTempC), "Max temperature must be at least the min temperature.");
        if (crop.MinHumidity > crop.MaxHumidity)
            ModelState.AddModelError(nameof(crop.MaxHumidity), "Max humidity must be at least the min humidity.");
        if (await db.Crops.AnyAsync(c => c.Name == crop.Name && c.Id != crop.Id))
            ModelState.AddModelError(nameof(crop.Name), "A crop with this name already exists.");

        if (!ModelState.IsValid) return View("Form", crop);

        if (crop.Id == 0) db.Crops.Add(crop);
        else db.Crops.Update(crop);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Saved crop \"{crop.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
