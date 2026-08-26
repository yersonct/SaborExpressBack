using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Data.SeedData
{
    public static class InitialManagerSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
        {
            var gerenteRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == RoleNames.Gerente);

            if (gerenteRole == null)
            {
                gerenteRole = new Role
                {
                    Name = RoleNames.Gerente,
                    Description = "Supervisa todas las sedes del restaurante",
                    RequiresCv = false,
                    Status = true
                };
                context.Roles.Add(gerenteRole);
                await context.SaveChangesAsync();
            }

            var yaExisteGerente = await context.UserRoles
                .AnyAsync(ur => ur.RoleId == gerenteRole.Id);

            if (yaExisteGerente)
                return;

            var email = configuration["InitialManager:Email"];
            var password = configuration["InitialManager:Password"];
            var fullName = configuration["InitialManager:FullName"];
            var document = configuration["InitialManager:Document"];
            var LastName = configuration["InitialManager:LastName"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "Faltan las credenciales del Gerente inicial en la configuración (InitialManager:Email / Password).");

            var employee = new Employee
            {
                Name = fullName ?? "Gerente General",
                Document = document ?? "0000000000",
                LastName = LastName ?? "Apellido",
                Status = "Activo",
                User = new User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Status = true,
                    EmailConfirmed = true,
                    MustChangePassword = false
                }
            };

            employee.User.UserRoles.Add(new UserRole { RoleId = gerenteRole.Id });

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
        }
    }
}