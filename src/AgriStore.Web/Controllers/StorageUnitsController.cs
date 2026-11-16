using AgriStore.Web.Data;
using AgriStore.Web.Models;
using AgriStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers;

public class StorageUnitsController(ApplicationDbContext db, InsightService insights) : Controller
{
    public async Task<IActionResult> Index() => View(await insights.GetUnitStatusesAsync());

    public async Task<IActionResult> Details(int id)
    {
        var unit = await db.StorageUnits.FindAsync(id);
        if (unit is null) return NotFound();

        // Last 7 days of readings for the chart, oldest first.
        var since = DateTime.Now.AddDays(-7);
        ViewBag.Readings = await db.Readings
            .Where(r => r.StorageUnitId == id && r.RecordedAt >= since)
            .OrderBy(r => r.RecordedAt)
            .ToListAsync();
        ViewBag.Lots = await insights.GetStoredLotsWithRiskAsync(id);
        return View(unit);
    }

    [Authorize(Roles = DbSeeder.AdminRole)]
    public IActionResult Create() => View("Form", new StorageUnit());

    [Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await db.StorageUnits.FindAsync(id);
        return unit is null ? NotFound() : View("Form", unit);
    }

    [HttpGet, Authorize(Roles = DbSeeder.AdminRole)]
    public IActionResult Save(int? id) => id is > 0 ? RedirectToAction(nameof(Edit), new { id }) : RedirectToAction(nameof(Create));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Save(StorageUnit unit)
    {
        unit.Name = unit.Name?.Trim() ?? "";
        if (await db.StorageUnits.AnyAsync(u => u.Name == unit.Name && u.Id != unit.Id))
            ModelState.AddModelError(nameof(unit.Name), "A storage unit with this name already exists.");

        if (unit.Id != 0)
        {
            var stored = await db.StockLots
                .Where(l => l.StorageUnitId == unit.Id && l.Status == LotStatus.Stored)
                .SumAsync(l => l.QuantityTonnes);
            if (unit.CapacityTonnes < stored)
                ModelState.AddModelError(nameof(unit.CapacityTonnes),
                    $"This unit already holds {stored:0.##} t. Capacity cannot go below that.");
        }

        if (!ModelState.IsValid) return View("Form", unit);

        if (unit.Id == 0) db.StorageUnits.Add(unit);
        else db.StorageUnits.Update(unit);
        await db.SaveChangesAsync();

        TempData["Message"] = $"Saved storage unit \"{unit.Name}\".";
        return RedirectToAction(nameof(Details), new { id = unit.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await db.StorageUnits.FindAsync(id);
        if (unit is null) return NotFound();

        if (await db.StockLots.AnyAsync(l => l.StorageUnitId == id))
        {
            TempData["Error"] = "This unit still has stock records. Move or delete them first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.StorageUnits.Remove(unit);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Deleted \"{unit.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
