namespace SaborExpress.Modules.Customers.DTOs
{
    public class RegisterCustomerDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
         public string? Document { get; set; } 
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
