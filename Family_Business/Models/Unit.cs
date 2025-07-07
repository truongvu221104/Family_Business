using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Unit
{
    public int UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
