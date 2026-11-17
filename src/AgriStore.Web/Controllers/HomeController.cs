using System.Diagnostics;
using AgriStore.Web.Data;
using AgriStore.Web.Models;
using AgriStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Controllers;

/// <summary>Public pages: the landing page and the error pages.</summary>
[AllowAnonymous]
public class HomeController(InsightService insights, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Crops = await db.Crops.AsNoTracking().OrderBy(c => c.ShelfLifeDays).ToListAsync();
        return View(await insights.GetDashboardAsync());
    }

    /// <summary>Styled page for 404, 403 and other status codes (wired via UseStatusCodePagesWithReExecute).</summary>
    [Route("Home/Status/{code:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
