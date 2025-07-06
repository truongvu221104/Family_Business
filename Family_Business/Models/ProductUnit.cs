using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class ProductUnit
{
    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public decimal FactorToBase { get; set; }

    public decimal CostPerUnit { get; set; }

    public decimal? SellPrice { get; set; }
    public virtual Product Product { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
