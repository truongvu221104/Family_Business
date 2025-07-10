using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
