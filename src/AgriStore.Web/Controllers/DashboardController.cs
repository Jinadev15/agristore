using AgriStore.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgriStore.Web.Controllers;

/// <summary>The signed-in overview: what needs action now and how each unit is doing.</summary>
public class DashboardController(InsightService insights) : Controller
{
    public async Task<IActionResult> Index() => View(await insights.GetDashboardAsync());
}
