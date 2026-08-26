namespace SaborExpress.Modules.Customers.DTOs
{
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}