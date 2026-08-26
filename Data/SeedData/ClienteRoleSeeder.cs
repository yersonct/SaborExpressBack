using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Data.SeedData
{
    public static class ClienteRoleSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var existe = await context.Roles.AnyAsync(r => r.Name == RoleNames.Cliente);

            if (existe)
                return;

            context.Roles.Add(new Role
            {
                Name = RoleNames.Cliente,
                Description = "Cliente registrado desde la app",
                RequiresCv = false,
                Status = true
            });

            await context.SaveChangesAsync();
        }
    }
}