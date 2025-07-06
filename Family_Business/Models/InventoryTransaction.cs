using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class InventoryTransaction
{
    public int TxId { get; set; }

    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public string TxType { get; set; } = null!;

    public int? PartyId { get; set; }

    public string? PartyType { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime TxDate { get; set; }

    public string? Note { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
