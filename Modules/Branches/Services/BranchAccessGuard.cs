using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Modules.Branches.Services
{
    public class BranchAccessGuard : IBranchAccessGuard
    {
        private readonly IAuthRepository _authRepository;

        public BranchAccessGuard(IAuthRepository authRepository)
            => _authRepository = authRepository;

        public async Task<int?> GetOwnBranchIdAsync(int userId)
        {
            var user = await _authRepository.GetByIdWithRelationsAsync(userId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            return user.Employee?.BranchId;
        }

        public async Task<bool> IsGerenteAsync(int userId)
        {
            var user = await _authRepository.GetByIdWithRelationsAsync(userId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            return user.HasRole(RoleNames.Gerente);
        }

        public async Task EnsureCanAccessBranchAsync(int branchId, int userId)
        {
            var user = await _authRepository.GetByIdWithRelationsAsync(userId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (user.HasRole(RoleNames.Gerente))
                return;

            if (user.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes operar sobre la sede a la que perteneces.");
        }
    }
}