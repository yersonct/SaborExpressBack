namespace SaborExpress.Modules.Branches.DTOs
{
    // Solo lo que un Cliente necesita para elegir de dónde pedir.
    // Sin conteo de empleados ni otros datos administrativos.
    public class PublicBranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}