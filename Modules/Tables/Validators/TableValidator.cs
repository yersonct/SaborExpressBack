// Modules/Tables/Validators/TableValidator.cs
using SaborExpress.Modules.Tables.DTOs;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Modules.Tables.Validators
{
    public class TableValidator
    {
        private readonly ITableRepository _tableRepository;

        public TableValidator(ITableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task ValidateCreateAsync(CreateTableDto dto)
        {
            if (dto.BranchId <= 0)
                throw new ArgumentException("Debe indicar una sucursal válida");

            if (dto.Number <= 0)
                throw new ArgumentException("El número de mesa debe ser mayor a cero");

            if (!await _tableRepository.BranchExistsAsync(dto.BranchId))
                throw new ArgumentException("La sucursal no existe");

            if (await _tableRepository.ExistsByNumberInBranchAsync(dto.BranchId, dto.Number))
                throw new ArgumentException("Ya existe una mesa con ese número en esta sucursal");
        }

        public async Task ValidateUpdateAsync(Table table, UpdateTableDto dto)
        {
            if (dto.Number <= 0)
                throw new ArgumentException("El número de mesa debe ser mayor a cero");

            if (await _tableRepository.ExistsByNumberInBranchAsync(table.BranchId, dto.Number, table.Id))
                throw new ArgumentException("Ya existe una mesa con ese número en esta sucursal");
        }

        public void ValidateStatusChange(Table table, UpdateTableStatusDto dto)
        {
            if (table.Status == dto.Status)
                throw new ArgumentException($"La mesa ya está en estado {dto.Status}");
        }

        public void ValidateDelete(Table table)
        {
            if (table.Status == TableStatus.Occupied)
                throw new ArgumentException(
                    "No se puede eliminar una mesa que está Ocupada. " +
                    "Cambia su estado a Disponible primero.");
        }
    }
}