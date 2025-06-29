using System;
using System.Collections.Generic;

namespace Family_Business.Models;

public partial class AuditLog
{
    public int AuditId { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public int? RecordId { get; set; }

    public DateTime ActionTime { get; set; }

    public string? Detail { get; set; }

    public virtual User User { get; set; } = null!;
}
