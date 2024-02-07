using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KEOPBackend.helpers.JwtTokenUtility
{
    public class JwtTokenUtility
    {
        private readonly IConfiguration _configuration;
        public JwtTokenUtility(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ClaimsPrincipal ValidatejwtToken(string jwtToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };
            SecurityToken validatedToken;
            ClaimsPrincipal claimsPrincipal = null;

            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
            }
            catch (SecurityTokenValidationException ex)
            {
                Console.WriteLine($"Token validation failed: {ex.Message}");
            }

            return claimsPrincipal;
        }
    }
}
