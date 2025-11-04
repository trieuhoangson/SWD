using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class BorrowDetail
{
    public int BorrowId { get; set; }

    public int BookId { get; set; }

    public int? Quantity { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual BorrowTransaction Borrow { get; set; } = null!;
}
