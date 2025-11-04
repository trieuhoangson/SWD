using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class LibraryCard
{
    public int CardId { get; set; }

    public int UserId { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string? Status { get; set; }

    public virtual User User { get; set; } = null!;
}
