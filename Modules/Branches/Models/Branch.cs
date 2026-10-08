    using System;
    using System.Collections.Generic;
    using SaborExpress.Modules.Employees.Models;

    namespace SaborExpress.Modules.Branches.Models
    {
        public class Branch
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public bool Status { get; set; } = true;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }

            public string? PublicKitchenCode { get; set; }

            public List<Employee> Employees { get; set; } = new();
        }
    }