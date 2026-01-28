using System.Security.Claims;
using SessionState.Examples;
using SessionState.Examples.Services;
using SessionState.Examples;
using SessionState.Examples.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDataProtection().SetApplicationName("SessionState.Examples");

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Configure SessionState with all features
// Note: WithKeyGenerator must be called FIRST before other configuration
builder.Services.AddSessionState<DemoKeyGenerator>()
    .WithInMemoryBackend()
    .WithKeepAlive(options =>
    {
        options.CheckInterval = TimeSpan.FromSeconds(30);
        options.RateLimitPermitLimit = 20;
    })
    .ConfigureOptions(options =>
    {
        options.CookieName = ".SessionState.Examples";
        options.CleanupInterval = TimeSpan.FromMinutes(1);
    })
    .ConfigureEvents(events =>
    {
        events.TrackPropertyChanges = true;
        events.AutoPersistOnPropertyChange = true;

        events.OnValueSet = args =>
            Console.WriteLine($"[Event] Value set for {args.StateType.Name} (Update: {args.IsUpdate})");

        events.OnValueCleared = args =>
            Console.WriteLine($"[Event] Value cleared for {args.StateType.Name}");

        events.OnValueChanged = args =>
            Console.WriteLine($"[Event] Property '{args.PropertyName}' changed on {args.StateType.Name}");
    });

// Register the custom cookie events in DI
builder.Services.AddScoped<DemoCookieEvents>();

// For authenticated scenarios demo
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";

        // Use EventsType instead of Events instance
        // SessionState will wrap these events and delegate calls to them
        options.EventsType = typeof(DemoCookieEvents);
    })
    .WithBlazorSessionState("Cookies"); // Integrate with SessionState

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// SessionState middleware - MUST be before MapRazorComponents
// This establishes the session during the initial HTTP request
app.UseBlazorSessionState();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Demo login endpoint (for demonstration purposes only)
app.MapGet("/demo-login", async (string user, HttpContext context) =>
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user),
        new(ClaimTypes.Name, char.ToUpperInvariant(user[0]) + user[1..])
    };

    var identity = new ClaimsIdentity(claims, "DemoAuth");
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync("Cookies", principal);

    return Results.Redirect("/authenticated-session");
});

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    return Results.Redirect("/");
});

app.Run();
