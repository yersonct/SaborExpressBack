using Microsoft.AspNetCore.Authorization;

namespace SaborExpress.Shared.Authorization
{
    // Marcador vacío: solo indica "este endpoint exige turno activo".
    // La lógica real vive en ActiveShiftHandler.
    public class ActiveShiftRequirement : IAuthorizationRequirement
    {
    }
}