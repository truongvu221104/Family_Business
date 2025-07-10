using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Customer
{
    public int CustomerID { get; set; }

    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
