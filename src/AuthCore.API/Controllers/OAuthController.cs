using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using AspNet.Security.OAuth.GitHub;
using AuthCore.Core.Interfaces;
using AuthCore.Core.DTOs;

namespace AuthCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public OAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin(string? tenantDomain = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { tenantDomain })
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(string? tenantDomain = null)
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
                return BadRequest(new { message = "Google authentication failed" });

            var email = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email not provided by Google" });

            // Extraer dominio del email si no se proporcionó tenantDomain
            var domain = tenantDomain ?? email.Split('@')[1];

            var oauthRequest = new OAuthLoginRequest
            {
                Email = email,
                Name = name ?? email,
                Provider = "Google",
                TenantDomain = domain
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var authResponse = await _authService.OAuthLoginAsync(oauthRequest, ipAddress);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during Google login", details = ex.Message });
        }
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin(string? tenantDomain = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback), new { tenantDomain })
        };
        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback(string? tenantDomain = null)
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync(GitHubAuthenticationDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
                return BadRequest(new { message = "GitHub authentication failed" });

            var email = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email not provided by GitHub" });

            // Extraer dominio del email si no se proporcionó tenantDomain
            var domain = tenantDomain ?? email.Split('@')[1];

            var oauthRequest = new OAuthLoginRequest
            {
                Email = email,
                Name = name ?? email,
                Provider = "GitHub",
                TenantDomain = domain
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var authResponse = await _authService.OAuthLoginAsync(oauthRequest, ipAddress);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during GitHub login", details = ex.Message });
        }
    }
}
