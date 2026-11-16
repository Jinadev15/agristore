using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgriStore.Tests;

/// <summary>End-to-end checks against the real app, on a throwaway SQLite database.</summary>
public class ApiTests : IClassFixture<ApiTests.AppFactory>
{
    public class AppFactory : WebApplicationFactory<Program>
    {
        private readonly string _db = Path.Combine(Path.GetTempPath(), $"agristore-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"DataSource={_db}");
            builder.UseSetting("Api:Key", "test-key");
            builder.UseEnvironment("Production");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { File.Delete(_db); } catch (IOException) { }
        }
    }

    private readonly AppFactory _factory;
    public ApiTests(AppFactory factory) => _factory = factory;

    private HttpClient Client(string? key = "test-key")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (key is not null) client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }

    [Fact]
    public async Task PostReading_ValidReading_Returns201()
    {
        var response = await Client().PostAsJsonAsync("/api/readings",
            new { storageUnitId = 1, temperatureC = 5.0, humidityPercent = 90.0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostReading_FutureDate_Returns400()
    {
        var response = await Client().PostAsJsonAsync("/api/readings",
            new { storageUnitId = 1, temperatureC = 5.0, humidityPercent = 90.0, recordedAt = DateTime.Now.AddDays(1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostReading_WrongKey_Returns401()
    {
        var response = await Client("wrong").PostAsJsonAsync("/api/readings",
            new { storageUnitId = 1, temperatureC = 5.0, humidityPercent = 90.0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPage_Returns404WithStyledPage()
    {
        var response = await Client(null).GetAsync("/no/such/page");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("This page does not exist", body);
    }

    [Fact]
    public async Task Landing_IsPublic()
    {
        var response = await Client(null).GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RequiresSignIn()
    {
        var response = await Client(null).GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.ToString());
    }
}
