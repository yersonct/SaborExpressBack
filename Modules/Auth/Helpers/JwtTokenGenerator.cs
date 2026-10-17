using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SaborExpress.Configuration;
using SaborExpress.Modules.Auth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.Auth.Helpers
{
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string Generate(User user, string identifier, bool actAsClient = false, IReadOnlyCollection<string>? effectiveRoles = null)
        {
            var claims = ConstruirClaims(user, identifier, actAsClient, effectiveRoles);
            var descriptor = ConstruirDescriptor(claims);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        private static List<Claim> ConstruirClaims(User user, string identifier, bool actAsClient, IReadOnlyCollection<string>? effectiveRoles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, identifier)
            };

            var roles = effectiveRoles ?? user.UserRoles.Select(ur => ur.Role.Name).ToList();
            foreach (var roleName in roles.Distinct())
                claims.Add(new Claim(ClaimTypes.Role, roleName));

            // Empleado operativo sin turno activo: usa la app como cliente
            if (actAsClient && !roles.Contains(RoleNames.Cliente))
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Cliente));

            // NUEVO: si el usuario es cliente, agregar su CustomerId al token
            if (user.Customer != null)
                claims.Add(new Claim("CustomerId", user.Customer.Id.ToString()));

        // NUEVO: si el usuario es empleado, agregar su EmployeeId al token
                    // (aprovechamos para resolver el mismo patron que faltaba en Employee)
                    if (user.Employee != null)
                    {
                        claims.Add(new Claim("EmployeeId", user.Employee.Id.ToString()));

                        // NUEVO: agregamos también el BranchId, para que la app móvil
                        // no necesite llamar a /api/Employees/{id} (bloqueado para
                        // roles operativos como Mesero/Cocinero/Cajero/Repartidor).
                        if (user.Employee.BranchId.HasValue)
                            claims.Add(new Claim("BranchId", user.Employee.BranchId.Value.ToString()));
                    }

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
