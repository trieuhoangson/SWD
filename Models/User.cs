using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public string Role { get; set; } = null!;
    public string Password { get; set; } = string.Empty;

    public virtual ICollection<BorrowTransaction> BorrowTransactionLibrarians { get; set; } = new List<BorrowTransaction>();

    public virtual ICollection<BorrowTransaction> BorrowTransactionUsers { get; set; } = new List<BorrowTransaction>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<LibraryCard> LibraryCards { get; set; } = new List<LibraryCard>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
