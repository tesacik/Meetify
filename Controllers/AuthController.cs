using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Meetify.Controllers;

[Route("auth")]
public class AuthController : Controller
{
	[HttpGet("login")]
	public IActionResult Login([FromQuery] string? returnUrl = "/")
	{
		var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
		return Challenge(props, "Google"); // kicks off Google OIDC
	}

	[HttpGet("logout")] // GET for demo simplicity
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return Redirect("/");
	}
}

