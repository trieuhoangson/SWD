using System;
using System.Collections.Generic;

namespace SWD.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int UserId { get; set; }

    public string? Content { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Response { get; set; }

    public virtual User User { get; set; } = null!;
}
