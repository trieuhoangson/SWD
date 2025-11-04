using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class BorrowTransaction
{
    public int BorrowId { get; set; }

    public int UserId { get; set; }

    public int? LibrarianId { get; set; }

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? Status { get; set; }

    public decimal? FineAmount { get; set; }

    public virtual ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();

    public virtual User? Librarian { get; set; }

    public virtual User User { get; set; } = null!;
}
