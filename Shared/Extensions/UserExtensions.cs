using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Shared.Extensions
{
    public static class UserExtensions
    {
        public static bool HasRole(this User user, string roleName)
            => user.UserRoles.Any(ur => ur.Role.Name == roleName);

        public static string GetDisplayName(this User user, string fallback)
           => user.Employee != null
               ? $"{user.Employee.Name} {user.Employee.LastName}".Trim()
               : user.Customer != null
                   ? $"{user.Customer.Name} {user.Customer.LastName}".Trim()
                   : fallback;
    }
}