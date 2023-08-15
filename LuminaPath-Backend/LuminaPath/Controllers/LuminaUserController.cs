using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Controllers
{
    [Route("api/[controller]")]
	[Authorize(AuthenticationSchemes = "Bearer")]
	[ApiController]
	public class LuminaUserController : ControllerBase
	{
		private readonly UserManager<LuminaUser> _userManager;
		private readonly ITokenGenerator _jwtService;

		public LuminaUserController(UserManager<LuminaUser> userManager, ITokenGenerator jwtService)
		{
			_userManager = userManager;
			_jwtService = jwtService;
		}

		[HttpGet("{username}")]
		[Authorize(Roles = "Administrator")]
		public async Task<ActionResult<UserDto>> GetUser(string username)
		{
			IdentityUser? user = await _userManager.FindByNameAsync(username);

			if (user == null)
			{
				return NotFound();
			}

			return new UserDto
			{
				UserName = user.UserName ?? "Error",
				Email = user.Email ?? "Error"
			};
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<ActionResult<UserDto>> PostUser(UserDto user)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var result = await _userManager.CreateAsync(
				new LuminaUser() { UserName = user.UserName, Email = user.Email },
				user.Password
			);

			if (!result.Succeeded)
			{
				return BadRequest(result.Errors);
			}

			user.Password = null;
			return Created("", user);
		}

		[HttpGet("refresh")]
		[AllowAnonymous]
		public async Task<IActionResult> Refresh()
		{
			if (!(Request.Cookies.TryGetValue("X-Username", out var userName) && Request.Cookies.TryGetValue("X-Refresh-Token", out var refreshToken)))
				return BadRequest();

			var user = _userManager.Users.FirstOrDefault(i => i.UserName == userName && i.RefreshToken == refreshToken);

			if (user == null)
				return BadRequest();

			var token = await _jwtService.CreateToken(user);

			user.RefreshToken = Guid.NewGuid().ToString();

			await _userManager.UpdateAsync(user);

			Response.Cookies.Append("X-Access-Token", token.Token, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
			Response.Cookies.Append("X-Username", user.UserName, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
			Response.Cookies.Append("X-Refresh-Token", user.RefreshToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });

			return Ok();
		}

		[HttpPost("BearerToken")]
		[AllowAnonymous]
		public async Task<ActionResult<AuthenticationResponse>> CreateBearerToken(UserCredentials request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest("Bad credentials");
			}

			var user = await _userManager.FindByNameAsync(request.UserName);

			if (user == null)
			{
				return BadRequest("Bad credentials");
			}

			var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

			if (!isPasswordValid)
			{
				return BadRequest("Bad credentials");
			}

			var token = await _jwtService.CreateToken(user);
			user.RefreshToken = Guid.NewGuid().ToString();
			await _userManager.UpdateAsync(user);
			Response.Cookies.Append("X-Access-Token", token.Token, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
			Response.Cookies.Append("X-Username", user.UserName, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
			Response.Cookies.Append("X-Refresh-Token", user.RefreshToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });

			return Ok();
		}
	}
}
