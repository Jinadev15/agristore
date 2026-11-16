using AgriStore.Web.Data;
using AgriStore.Web.Middleware;
using AgriStore.Web.Services;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQLite by default (no install needed). Set "DatabaseProvider": "SqlServer" to use SQL Server.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Configuration["DatabaseProvider"] == "SqlServer")
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString);
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity: login, register and roles (Admin manages storage units and crops).
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// Our own services (dependency injection).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRiskService, RiskService>();
builder.Services.AddScoped<InsightService>();

// Every controller action needs a signed-in user unless it says [AllowAnonymous].
// (A filter rather than a fallback policy, so unknown URLs still get a proper 404 page.)
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AuthorizeFilter()));
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var india = new CultureInfo("en-IN");
    options.DefaultRequestCulture = new RequestCulture(india);
    options.SupportedCultures = options.SupportedUICultures = [india];
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseRequestLocalization();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program;
