using Data.Classes;
using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Data.Services
{
    public class JwtService : ITokenGenerator
	{
		private const int EXPIRATION_MINUTES = 1;

		private readonly IConfiguration _configuration;
		private readonly UserManager<LuminaUser> _userManager;

		public JwtService(IConfiguration configuration, UserManager<LuminaUser> userManager)
		{
			_configuration = configuration;
			_userManager = userManager;
		}

		public async Task<AuthenticationResponse> CreateToken(LuminaUser user)
		{
			var expiration = DateTime.UtcNow.AddMinutes(EXPIRATION_MINUTES);

			var roles = await _userManager.GetRolesAsync(user);
			string role = roles.FirstOrDefault() ?? string.Empty;

			var token = CreateJwtToken(
				CreateClaims(user, role),
				CreateSigningCredentials(),
				expiration
			);

			var tokenHandler = new JwtSecurityTokenHandler();

			return new AuthenticationResponse
			{
				Token = tokenHandler.WriteToken(token),
				Expiration = expiration
			};
		}

		private JwtSecurityToken CreateJwtToken(Claim[] claims, SigningCredentials credentials, DateTime expiration) =>
			new JwtSecurityToken(
				_configuration["Jwt:Issuer"],
				_configuration["Jwt:Audience"],
				claims,
				expires: expiration,
				signingCredentials: credentials
			);

		private Claim[] CreateClaims(LuminaUser user, string role) =>
			new[] {
				new Claim(JwtRegisteredClaimNames.Sub, user.Id),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Role, role),
				new Claim(ClaimTypes.Name, user.UserName),
				new Claim(ClaimTypes.Email, user.Email)
			};

		private SigningCredentials CreateSigningCredentials() =>
			new SigningCredentials(
				new SymmetricSecurityKey(
					Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])
				),
				SecurityAlgorithms.HmacSha256
			);
	}
}
