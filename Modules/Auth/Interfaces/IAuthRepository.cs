using SaborExpress.Modules.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByDocumentAsync(string document);
        Task<User?> GetByIdentifierAsync(string identifier);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
