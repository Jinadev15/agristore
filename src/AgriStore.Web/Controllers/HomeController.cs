using System.Diagnostics;
using AgriStore.Web.Models;
using AgriStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStore.Web.Controllers;

public class HomeController(InsightService insights) : Controller
{
    public async Task<IActionResult> Index() => View(await insights.GetDashboardAsync());

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
