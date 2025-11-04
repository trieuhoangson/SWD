using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class Book
{
    public int BookId { get; set; }

    public int CatId { get; set; }

    public string Title { get; set; } = null!;

    public string? Author { get; set; }

    public string? Publisher { get; set; }

    public int? PublicationYear { get; set; }

    public int? Quantity { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();

    public virtual Category Cat { get; set; } = null!;
}
