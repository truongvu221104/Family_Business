using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public int BaseUnitId { get; set; }

    public decimal MarkupPercent { get; set; }

    public string? Note { get; set; }

    public virtual Unit BaseUnit { get; set; } = null!;

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<ProductUnit> ProductUnits { get; set; } = new List<ProductUnit>();
}
