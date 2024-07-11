using MeloStats.Data;
using MeloStats.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MeloStats.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var redirectUrl = Url.Action("SpotifyResponse", "Users");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Spotify", redirectUrl);
            return Challenge(properties, "Spotify");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SpotifyResponse()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = info.Principal.FindFirstValue(ClaimTypes.Email),
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                    SpotifyUserId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }

                createResult = await _userManager.AddLoginAsync(user, info);
                if (!createResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }
            }

            // Retrieve the token information
            var accessToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
            var refreshToken = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
            var tokenType = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "token_type")?.Value;
            var expiresIn = int.Parse(info.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_in")?.Value ?? "0");

            _logger.LogInformation("Tokens: {accessToken} {refreshToken}.", accessToken, refreshToken);

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                var spotifyToken = new SpotifyToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenType = tokenType,
                    CreatedAt = DateTime.Now,
                    UserId = user.Id,
                    User = user
                };

                // Check if the user already has tokens
                var existingToken = _context.SpotifyTokens.FirstOrDefault(t => t.UserId == user.Id);
                if (existingToken != null)
                {
                    // Update existing token if needed
                    existingToken.AccessToken = spotifyToken.AccessToken;
                    existingToken.RefreshToken = spotifyToken.RefreshToken;
                    existingToken.TokenType = spotifyToken.TokenType;
                    existingToken.CreatedAt = spotifyToken.CreatedAt;

                    _context.SpotifyTokens.Update(existingToken);
                }
                else
                {
                    // Add new token
                    _context.SpotifyTokens.Add(spotifyToken);
                }

                _context.SaveChanges();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }



    }
}
