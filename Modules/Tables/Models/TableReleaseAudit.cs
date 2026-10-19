namespace SaborExpress.Modules.Tables.Models
{
    // Registro independiente de los pedidos y pagos: una liberación no altera la contabilidad.
    public sealed class TableReleaseAudit
    {
        public long Id { get; set; }
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int BranchId { get; set; }
        public Guid? PreviousSessionId { get; set; }
        public int ActorUserId { get; set; }
        public bool Administrative { get; set; }
        public string Reason { get; set; } = "";
        public string OrderIdsJson { get; set; } = "[]";
        public decimal OutstandingAmount { get; set; }
        public DateTime ReleasedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
