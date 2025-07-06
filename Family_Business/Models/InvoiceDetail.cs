using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class InvoiceDetail
{
    public int DetailId { get; set; }

    public int InvoiceId { get; set; }

    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Total { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
