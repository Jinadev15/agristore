using AgriStore.Web.Data;
using AgriStore.Web.Models;
using AgriStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers;

public class StockLotsController(ApplicationDbContext db, InsightService insights) : Controller
{
    /// <summary>Stored lots with risk. Optional filters: minimum risk level and a farmer/crop search.</summary>
    public async Task<IActionResult> Index(RiskLevel? minRisk, string? search)
    {
        var lots = await insights.GetStoredLotsWithRiskAsync();

        if (minRisk is not null)
            lots = lots.Where(x => x.Risk.Level >= minRisk).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            lots = lots.Where(x => x.Lot.FarmerName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || x.Lot.Crop!.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        ViewBag.MinRisk = minRisk;
        ViewBag.Search = search;
        ViewBag.History = await db.StockLots.Include(l => l.Crop).Include(l => l.StorageUnit)
            .Where(l => l.Status != LotStatus.Stored)
            .OrderByDescending(l => l.StoredOn).Take(20).ToListAsync();
        return View(lots);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lot = await insights.GetLotWithRiskAsync(id);
        return lot is null ? NotFound() : View(lot);
    }

    public async Task<IActionResult> Create(int? storageUnitId)
    {
        await FillDropdownsAsync();
        return View("Form", new StockLot { StorageUnitId = storageUnitId ?? 0 });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lot = await db.StockLots.FindAsync(id);
        if (lot is null) return NotFound();
        await FillDropdownsAsync();
        return View("Form", lot);
    }

    /// <summary>Refresh or Back after a failed save lands here: send the user back to the form.</summary>
    [HttpGet]
    public IActionResult Save(int? id) => id is > 0 ? RedirectToAction(nameof(Edit), new { id }) : RedirectToAction(nameof(Create));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(StockLot lot)
    {
        lot.FarmerName = lot.FarmerName?.Trim() ?? "";
        if (lot.StoredOn.Date > DateTime.Today)
            ModelState.AddModelError(nameof(lot.StoredOn), "Stored date cannot be in the future.");

        var unit = await db.StorageUnits.FindAsync(lot.StorageUnitId);
        if (unit is null)
            ModelState.AddModelError(nameof(lot.StorageUnitId), "Choose a storage unit.");
        else
        {
            // Capacity check: everything else already stored in this unit, plus this lot.
            var used = await db.StockLots
                .Where(l => l.StorageUnitId == unit.Id && l.Status == LotStatus.Stored && l.Id != lot.Id)
                .SumAsync(l => l.QuantityTonnes);
            if (used + lot.QuantityTonnes > unit.CapacityTonnes)
                ModelState.AddModelError(nameof(lot.QuantityTonnes),
                    $"Not enough space: {unit.Name} has {unit.CapacityTonnes - used:0.##} t free.");
        }

        if (!ModelState.IsValid)
        {
            await FillDropdownsAsync();
            return View("Form", lot);
        }

        if (lot.Id == 0)
        {
            lot.Status = LotStatus.Stored;
            db.StockLots.Add(lot);
        }
        else
        {
            // Copy only the fields the form edits; status changes go through SetStatus.
            var existing = await db.StockLots.FindAsync(lot.Id);
            if (existing is null) return NotFound();
            existing.FarmerName = lot.FarmerName;
            existing.CropProfileId = lot.CropProfileId;
            existing.StorageUnitId = lot.StorageUnitId;
            existing.QuantityTonnes = lot.QuantityTonnes;
            existing.StoredOn = lot.StoredOn;
        }
        await db.SaveChangesAsync();

        TempData["Message"] = "Stock lot saved.";
        return RedirectToAction(nameof(Details), new { id = lot.Id });
    }

    /// <summary>Marks a lot as sold/dispatched or spoiled, which takes it out of active stock.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, LotStatus status)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var lot = await db.StockLots.FindAsync(id);
        if (lot is null) return NotFound();

        if (status == LotStatus.Stored && lot.Status != LotStatus.Stored)
        {
            // Moving a closed lot back into stock must still fit in its unit.
            var unit = await db.StorageUnits.FindAsync(lot.StorageUnitId);
            var used = await db.StockLots
                .Where(l => l.StorageUnitId == lot.StorageUnitId && l.Status == LotStatus.Stored)
                .SumAsync(l => l.QuantityTonnes);
            if (unit is not null && used + lot.QuantityTonnes > unit.CapacityTonnes)
            {
                TempData["Error"] = $"{unit.Name} has only {unit.CapacityTonnes - used:0.##} t free, so lot #{id} cannot go back into stock.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        lot.Status = status;
        await db.SaveChangesAsync();
        TempData["Message"] = status switch
        {
            LotStatus.Dispatched => $"Lot #{id} marked dispatched.",
            LotStatus.Spoiled => $"Lot #{id} recorded as spoiled.",
            _ => $"Lot #{id} is back in stock."
        };

        // Go back to the page the button was on (overview, stock list or lot), never off-site.
        var referer = Request.Headers.Referer.ToString();
        return Uri.TryCreate(referer, UriKind.Absolute, out var from) && from.Host == Request.Host.Host && Url.IsLocalUrl(from.PathAndQuery)
            ? LocalRedirect(from.PathAndQuery)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = DbSeeder.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var lot = await db.StockLots.FindAsync(id);
        if (lot is null) return NotFound();

        db.StockLots.Remove(lot);
        await db.SaveChangesAsync();
        TempData["Message"] = $"Lot #{id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillDropdownsAsync()
    {
        ViewBag.Crops = new SelectList(await db.Crops.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Units = new SelectList(await db.StorageUnits.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
    }
}
