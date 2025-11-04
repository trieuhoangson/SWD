using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SWD.Models;

public partial class LibraryManagementSystemContext : DbContext
{
    public static LibraryManagementSystemContext Ins = new LibraryManagementSystemContext();

    public LibraryManagementSystemContext()
    {
        if (Ins == null) Ins = this;
    }

    public LibraryManagementSystemContext(DbContextOptions<LibraryManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BorrowDetail> BorrowDetails { get; set; }

    public virtual DbSet<BorrowTransaction> BorrowTransactions { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<LibraryCard> LibraryCards { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    // => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=LibraryManagementSystem; Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(config.GetConnectionString("MyCnn"));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__Book__3DE0C2073B3DEBE0");

            entity.ToTable("Book");

            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.Publisher).HasMaxLength(100);
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Cat).WithMany(p => p.Books)
                .HasForeignKey(d => d.CatId)
                .HasConstraintName("FK__Book__CatId__3B75D760");
        });

        modelBuilder.Entity<BorrowDetail>(entity =>
        {
            entity.HasKey(e => new { e.BorrowId, e.BookId }).HasName("PK__BorrowDe__214BF41F058E3A81");

            entity.ToTable("BorrowDetail");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Book).WithMany(p => p.BorrowDetails)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BorrowDet__BookI__49C3F6B7");

            entity.HasOne(d => d.Borrow).WithMany(p => p.BorrowDetails)
                .HasForeignKey(d => d.BorrowId)
                .HasConstraintName("FK__BorrowDet__Borro__48CFD27E");
        });

        modelBuilder.Entity<BorrowTransaction>(entity =>
        {
            entity.HasKey(e => e.BorrowId).HasName("PK__BorrowTr__4295F83FFB48EA66");

            entity.ToTable("BorrowTransaction");

            entity.Property(e => e.BorrowDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.FineAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ReturnDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Borrowed");

            entity.HasOne(d => d.Librarian).WithMany(p => p.BorrowTransactionLibrarians)
                .HasForeignKey(d => d.LibrarianId)
                .HasConstraintName("FK__BorrowTra__Libra__44FF419A");

            entity.HasOne(d => d.User).WithMany(p => p.BorrowTransactionUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BorrowTra__UserI__440B1D61");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CatId).HasName("PK__Category__6A1C8AFA9C2D360F");

            entity.ToTable("Category");

            entity.Property(e => e.CatName).HasMaxLength(100);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDD61E40F618");

            entity.ToTable("Feedback");

            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Response).HasMaxLength(1000);

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__UserId__571DF1D5");
        });

        modelBuilder.Entity<LibraryCard>(entity =>
        {
            entity.HasKey(e => e.CardId).HasName("PK__LibraryC__55FECDAEF2C14935");

            entity.ToTable("LibraryCard");

            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.IssueDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.User).WithMany(p => p.LibraryCards)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__LibraryCa__UserI__4E88ABD4");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Log__5E548648D48D308A");

            entity.ToTable("Log");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TimeStamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Log__UserId__5AEE82B9");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A3833159BE6");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Success");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__UserId__534D60F1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C1CFC5032");

            entity.ToTable("User");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(10);
            entity.Property(e => e.Role).HasMaxLength(10);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
