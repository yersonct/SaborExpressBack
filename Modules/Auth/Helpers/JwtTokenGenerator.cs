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
            var claims = ConstruirClaims(user, identifier);
            var descriptor = ConstruirDescriptor(claims);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        private static List<Claim> ConstruirClaims(User user, string identifier)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, identifier)
            };

            foreach (var userRole in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));

            // NUEVO: si el usuario es cliente, agregar su CustomerId al token
            if (user.Customer != null)
                claims.Add(new Claim("CustomerId", user.Customer.Id.ToString()));

            // NUEVO: si el usuario es empleado, agregar su EmployeeId al token
            // (aprovechamos para resolver el mismo patron que faltaba en Employee)
            if (user.Employee != null)
                claims.Add(new Claim("EmployeeId", user.Employee.Id.ToString()));

            return claims;
        }

        private SecurityTokenDescriptor ConstruirDescriptor(List<Claim> claims)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
        }
    }
}
