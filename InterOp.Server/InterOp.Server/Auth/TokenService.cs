using InterOp.Server.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InterOp.Server.Auth
{
    public sealed class TokenService
    {
        private readonly IConfiguration _cfg;
        public TokenService(IConfiguration cfg) => _cfg = cfg;

        public (string token, DateTime expiresUtc) CreateAccessToken(AppUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(int.Parse(_cfg["Jwt:AccessTokenMinutes"] ?? "30"));

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

            var jwt = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return (token, expires);
        }

        public (string token, DateTime expiresUtc) CreateRefreshToken()
        {
            var bytes = new byte[64];
            Random.Shared.NextBytes(bytes);
            var token = Convert.ToBase64String(bytes);
            var expires = DateTime.UtcNow.AddDays(int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "7"));
            return (token, expires);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!)),
                ValidIssuer = _cfg["Jwt:Issuer"],
                ValidAudience = _cfg["Jwt:Audience"],
                ValidateLifetime = false 
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                return handler.ValidateToken(token, tokenValidationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
