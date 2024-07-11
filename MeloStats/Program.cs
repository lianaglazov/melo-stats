using MeloStats.Data;
using MeloStats.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeloStats.Models;
using AspNet.Security.OAuth.Spotify;
using System.Security.Claims;
using Azure.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
.AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register Spotify services
var spotifyConfig = builder.Configuration.GetSection("Spotify");
builder.Services.AddSingleton(new SpotifyAuthService(spotifyConfig["ClientId"], spotifyConfig["ClientSecret"]));
builder.Services.AddTransient<SpotifyApiService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddSpotify(options =>
{
    options.ClientId = builder.Configuration["Spotify:ClientId"];
    options.ClientSecret = builder.Configuration["Spotify:ClientSecret"];
    options.CallbackPath = new PathString("/callback");
    options.AuthorizationEndpoint = "https://accounts.spotify.com/authorize";
    options.TokenEndpoint = "https://accounts.spotify.com/api/token";
    options.Scope.Add("user-read-private");
    options.Scope.Add("user-read-email");
    options.Scope.Add("user-top-read");
    options.SaveTokens = true;

    options.Events.OnCreatingTicket = async context =>
    {
        var email = context.Principal.FindFirstValue(ClaimTypes.Email);
        var _userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var userToken = await db.SpotifyTokens.SingleOrDefaultAsync(t => t.UserId == user.Id);
            if (userToken == null)
            {
                userToken = new SpotifyToken { UserId = user.Id };
                db.SpotifyTokens.Add(userToken);
            }

            userToken.AccessToken = context.AccessToken;
            userToken.RefreshToken = context.RefreshToken;
            userToken.ExpiresIn = 3600;
            userToken.TokenType = "Bearer";
            userToken.CreatedAt = DateTime.UtcNow;
            user.SpotifyUserId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            user.SpotifyTokens = new List<SpotifyToken>
            {
                userToken
            };
            await _userManager.UpdateAsync(user);
            await db.SaveChangesAsync();
        }
    };
})
.AddCookie();

builder.Services.AddLogging(
    builder =>
    {
        builder.AddConsole(); 
        builder.AddDebug();   
        
    });
builder.Services.AddHttpClient();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
