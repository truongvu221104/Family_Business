using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? InvoiceId { get; set; }

    public int? SupplierId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string Type { get; set; } = null!;

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    public virtual Supplier? Supplier { get; set; }
}
