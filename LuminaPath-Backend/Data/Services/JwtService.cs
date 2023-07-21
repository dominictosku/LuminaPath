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

		public JwtService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public AuthenticationResponse CreateToken(LuminaUser user)
		{
			var expiration = DateTime.UtcNow.AddMinutes(EXPIRATION_MINUTES);

			var token = CreateJwtToken(
				CreateClaims(user),
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

		private Claim[] CreateClaims(LuminaUser user) =>
			new[] {
				new Claim(JwtRegisteredClaimNames.Sub, user.Id),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
				new Claim(ClaimTypes.NameIdentifier, user.Id),
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
