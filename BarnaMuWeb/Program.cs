// BarnaMu Web - Program.cs
// Standalone ASP.NET Core Razor Pages site for the BarnaMu MU Online private server.
// Connects directly to the OpenMU PostgreSQL database (data."Account", data."Character",
// config."CharacterClass") using Npgsql + Dapper. Account passwords are hashed with
// BCrypt.Net-Next to be 100% compatible with OpenMU's authentication.

using BarnaMu.Web.Data;
using BarnaMu.Web.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Bind the BarnaMu options section to a strongly-typed object so pages and services can
// inject IOptions<BarnaMuOptions>.
builder.Services.Configure<BarnaMuOptions>(builder.Configuration.GetSection("BarnaMu"));

// Razor Pages — built-in anti-forgery is automatic for all form posts.
builder.Services.AddRazorPages();

// Application services.
builder.Services.AddSingleton<BarnaMuDb>();
builder.Services.AddSingleton<ServerStatusService>();

// Basic rate limiting — protects /Register and /ReportBug from abuse without external deps.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 5 registrations per IP per hour. Adjust if it bites legitimate users.
    options.AddPolicy("register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    // 10 bug reports per IP per hour.
    options.AddPolicy("bugreport", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

var app = builder.Build();

// Ensure the website's own schema/table for bug reports exists. This is the ONLY DDL the
// website performs against the database, and it lives in its own "web" schema so it never
// collides with OpenMU's migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BarnaMuDb>();
    await db.EnsureWebSchemaAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Comment HSTS out while serving HTTP-only behind DDNS; turn back on once TLS is set up.
    // app.UseHsts();
}

// app.UseHttpsRedirection(); // re-enable once we add HTTPS / put Cloudflare in front.
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
