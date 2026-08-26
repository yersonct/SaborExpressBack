namespace SaborExpress.Modules.Customers.DTOs
{
    public class UpdateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}