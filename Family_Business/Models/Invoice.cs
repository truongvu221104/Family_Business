using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int CustomerId { get; set; }

    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    public int CreatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
