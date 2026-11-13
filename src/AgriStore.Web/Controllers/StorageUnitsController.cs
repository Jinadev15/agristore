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

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Save(StorageUnit unit)
    {
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
