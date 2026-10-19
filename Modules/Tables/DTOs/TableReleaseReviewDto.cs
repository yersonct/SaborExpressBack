namespace SaborExpress.Modules.Tables.DTOs
{
    public sealed class TableReleaseOrderDto
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsPaid { get; set; }
    }

    public sealed class TableReleaseReviewDto
    {
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int BranchId { get; set; }
        public Guid? CurrentSessionId { get; set; }
        public bool IsOccupied { get; set; }
        // La fecha de creación de la MESA no identifica el comienzo de la ocupación.
        // Solo los pedidos vinculados permiten confirmar que es una sesión anterior.
        public bool IsPreviousDay { get; set; }
        public bool IsOrphan { get; set; }
        public bool CanReleaseNormally { get; set; }
        public bool RequiresAdministrativeRelease { get; set; }
        public decimal OutstandingAmount { get; set; }
        public List<TableReleaseOrderDto> Orders { get; set; } = new();
    }

    public sealed class ReleasePreviousTableDto
    {
        // Protección ante pantallas obsoletas: se comprueba otra vez dentro de la transacción.
        public Guid? ExpectedSessionId { get; set; }
        public bool Administrative { get; set; }
        public string? Reason { get; set; }
    }
}
