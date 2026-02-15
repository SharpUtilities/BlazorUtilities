using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using SessionAndState.Examples;
using SessionAndState.Examples.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7016",
                "https://localhost:5001",
                "http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDataProtection().SetApplicationName("BlazorState.Examples");

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// JWT generation needs access to the current HttpContext/User
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ApiDelegatingHandler>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri("https://localhost:7016/");
}).AddHttpMessageHandler<ApiDelegatingHandler>();

// Configure BlazorState with all features
// Note: WithKeyGenerator must be called FIRST before other configuration
builder.Services.AddSessionAndState<DemoKeyGenerator>(options =>
    {
        options.CleanupInterval = TimeSpan.FromMinutes(10);
    })
    .WithInMemoryBackend()
    .WithKeepAlive(options =>
    {
        options.CheckInterval = TimeSpan.FromSeconds(30);
        options.RateLimitPermitLimit = 20;
        options.CorsPolicyName = "DemoCorsPolicy";
    })
    .WithAnonymousCookieSession(options =>
    {
        options.CookieName = ".SessionAndState.Examples";
        options.MaxAge = TimeSpan.FromDays(7);
    })
    .WithAuthCookieClaimSessionKey("Cookies");

// Register the custom cookie events in DI
builder.Services.AddScoped<DemoCookieEvents>();

// For authenticated scenarios demo
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";

        // Use EventsType instead of Events instance
        // BlazorState will wrap these events and delegate calls to them
        options.EventsType = typeof(DemoCookieEvents);
    });

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

app.UseCors("DemoCorsPolicy");

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// BlazorState middleware - MUST be before MapRazorComponents
// This establishes the session during the initial HTTP request
app.UseSessionAndState();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Demo login endpoint (for demonstration purposes only)
app.MapGet("/demo-login", async (string user, HttpContext context, IJwtTokenService jwtTokenService) =>
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user),
        new(ClaimTypes.Name, char.ToUpperInvariant(user[0]) + user[1..])
    };

    var identity = new ClaimsIdentity(claims, "DemoAuth");
    var principal = new ClaimsPrincipal(identity);

    // JwtTokenService reads HttpContext.User; set it for this request so it can generate the token
    context.User = principal;

    var jwt = jwtTokenService.GenerateToken();
    if (!string.IsNullOrWhiteSpace(jwt))
    {
        // Store the JWT as a claim so it travels with the auth cookie
        identity.AddClaim(new Claim("jwt", jwt));
    }

    await context.SignInAsync("Cookies", principal);

    return Results.Redirect("/authenticated-session");
});

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    return Results.Redirect("/");
});

app.Run();
