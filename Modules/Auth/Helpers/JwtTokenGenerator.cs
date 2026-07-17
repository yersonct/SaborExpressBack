using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SaborExpress.Configuration;
using SaborExpress.Modules.Auth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaborExpress.Modules.Auth.Helpers
{
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string Generate(User user, string identifier)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, identifier),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),

                Expires = DateTime.UtcNow.AddMinutes(
                    _jwtSettings.ExpirationInMinutes),

                Issuer = _jwtSettings.Issuer,

                Audience = _jwtSettings.Audience,

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var handler = new JwtSecurityTokenHandler();

            var token = handler.CreateToken(descriptor);

            return handler.WriteToken(token);
        }
    }
}