using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using AspNet.Security.OAuth.GitHub;

namespace AuthCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        
        if (!result.Succeeded)
            return BadRequest(new { message = "Google authentication failed" });

        var email = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new
        {
            message = "Google authentication successful",
            email = email,
            name = name
        });
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GitHubCallback))
        };
        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GitHubAuthenticationDefaults.AuthenticationScheme);
        
        if (!result.Succeeded)
            return BadRequest(new { message = "GitHub authentication failed" });

        var email = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = result.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new
        {
            message = "GitHub authentication successful",
            email = email,
            name = name
        });
    }
}
