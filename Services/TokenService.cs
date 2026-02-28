using Microsoft.IdentityModel.Tokens;
using NSprob.Server.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NSprob.Server.Services
{
    public class TokenService
    {
        private readonly IConfiguration _cfg;
        public TokenService(IConfiguration cfg) => _cfg = cfg;

        public string Generate(User user)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _cfg["Jwt:Key"] ?? "NSprob-Change-This-Key-In-Production-256bit!!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:   "NSprob",
                audience: "NSprob",
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                },
                expires:            DateTime.UtcNow.AddDays(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
