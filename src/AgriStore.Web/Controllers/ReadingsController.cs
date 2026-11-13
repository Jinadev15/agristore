using AgriStore.Web.Data;
using AgriStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers;

/// <summary>Sensor readings. Devices normally post through the API; this page allows manual entry too.</summary>
public class ReadingsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? storageUnitId)
    {
        var readings = await db.Readings.Include(r => r.StorageUnit)
            .Where(r => storageUnitId == null || r.StorageUnitId == storageUnitId)
            .OrderByDescending(r => r.RecordedAt)
            .Take(100)
            .ToListAsync();

        ViewBag.Units = new SelectList(await db.StorageUnits.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", storageUnitId);
        return View(readings);
    }

    public async Task<IActionResult> Create(int? storageUnitId)
    {
        ViewBag.Units = new SelectList(await db.StorageUnits.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View(new SensorReading { StorageUnitId = storageUnitId ?? 0, RecordedAt = DateTime.Now });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SensorReading reading)
    {
        if (!await db.StorageUnits.AnyAsync(u => u.Id == reading.StorageUnitId))
            ModelState.AddModelError(nameof(reading.StorageUnitId), "Choose a storage unit.");
        if (reading.RecordedAt > DateTime.Now.AddMinutes(5))
            ModelState.AddModelError(nameof(reading.RecordedAt), "Reading time cannot be in the future.");

        if (!ModelState.IsValid)
        {
            ViewBag.Units = new SelectList(await db.StorageUnits.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
            return View(reading);
        }

        db.Readings.Add(reading);
        await db.SaveChangesAsync();
        TempData["Message"] = "Reading recorded.";
        return RedirectToAction("Details", "StorageUnits", new { id = reading.StorageUnitId });
    }
}
