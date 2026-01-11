using GamingPlatform.Hubs;
using GamingPlatform.Hubs.Morpion;
using GamingPlatform.Hubs.Puissance4;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddSingleton<GamingPlatform.Services.LobbyService>();
builder.Services.AddSingleton<GamingPlatform.Services.GameHistoryService>();
builder.Services.AddSingleton<GamingPlatform.Hubs.Morpion.GameState>();
builder.Services.AddSingleton<GamingPlatform.Hubs.Puissance4.GameState>();

// Ajouter les services de session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Dur�e d'inactivit�
    options.Cookie.HttpOnly = true; // Acc�s uniquement c�t� serveur
    options.Cookie.IsEssential = true; // Requis pour la session
});

// Ajouter SignalR
builder.Services.AddSignalR();
// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:5176")  // Remplacez par l'URL de votre frontend si nécessaire
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Permet l'envoi de cookies avec la requête
    });
});


var app = builder.Build();

// Localization / cultures
var supportedCultures = new[]
{
    new CultureInfo("fr-FR"),
    new CultureInfo("en-US"),
    new CultureInfo("es-ES"),
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fr-FR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
};
// Keep cookie provider first so selection persists and wins over Accept-Language
localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();  // Cette ligne doit être avant UseRouting()

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

// Activer les sessions AVANT tout middleware qui utilise les sessions
app.UseSession();
app.UseWebSockets();

// Middleware pour g�rer les identifiants uniques des sessions
app.Use(async (context, next) =>
{
    if (!context.Request.Cookies.ContainsKey("UserSessionId"))
    {
        var sessionId = Guid.NewGuid().ToString();
        context.Response.Cookies.Append("UserSessionId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true // Activer uniquement si vous utilisez HTTPS
        });

        context.Session.SetString("SessionId", sessionId);
    }
    else
    {
        context.Session.SetString("SessionId", context.Request.Cookies["UserSessionId"]);
    }

    await next.Invoke();
});

app.UseAuthorization();

// Configurer SignalR

app.MapHub<GamingPlatform.Hubs.SpeedTypingHub>("/speedTypingHub");
app.MapHub<GamingPlatform.Hubs.Morpion.MorpionHub>("/morpionHub");
app.MapHub<GamingPlatform.Hubs.Puissance4.Puissance4Hub>("/puissance4Hub");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<LobbyHub>("/lobbyHub");
app.MapHub<GameHub>("/gameHub");

app.MapControllerRoute(
    name: "puissance4-game",
    pattern: "Puissance4/Game/{id}/{player}",
    defaults: new { controller = "Puissance4", action = "Game" });


app.Run();
