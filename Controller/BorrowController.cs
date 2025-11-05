using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SWD
{
    public class BorrowController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public BorrowController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Borrow/Index
        public async Task<IActionResult> Index(int? userId = null)
        {
            var borrows = _context.BorrowTransactions
                .Include(b => b.User)
                .Include(b => b.BorrowDetails)
                .ThenInclude(d => d.Book)
                .AsQueryable();

            // If userId not provided, default to current logged-in user
            if (userId == null)
            {
                var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var uid)) userId = uid;
            }

            if (userId != null)
            {
                borrows = borrows.Where(b => b.UserId == userId);
            }

            return View(await borrows.ToListAsync());
        }

        // POST: Borrow/Approve/5 (admin approves -> Borrowing)
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var borrow = await _context.BorrowTransactions.Include(b => b.BorrowDetails)
                .FirstOrDefaultAsync(b => b.BorrowId == id);
            if (borrow == null) return NotFound();

            if (borrow.Status == "Processing")
            {
                borrow.Status = "Borrowing";
                // For simplicity, assign LibrarianId to current user if available, else admin id 1
                int librarianId = 1;
                var claimId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out var cid)) librarianId = cid;
                borrow.LibrarianId = librarianId;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Borrow/Reject/5 (admin cancels request)
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var borrow = await _context.BorrowTransactions.FirstOrDefaultAsync(b => b.BorrowId == id);
            if (borrow == null) return NotFound();

            if (borrow.Status == "Processing")
            {
                borrow.Status = "Rejected";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Borrow/Renew/5
        public async Task<IActionResult> Renew(int id)
        {
            var borrow = await _context.BorrowTransactions.FindAsync(id);
            if (borrow == null) return NotFound();

            if (borrow.ReturnDate != null)
                ModelState.AddModelError("", "This book has already been returned.");

            if (borrow.DueDate.AddDays(7) < DateTime.Now.AddMonths(6))
                borrow.DueDate = borrow.DueDate.AddDays(7);

            _context.Update(borrow);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Borrow/Return/5
        public async Task<IActionResult> ReturnBook(int id)
        {
            var borrow = await _context.BorrowTransactions.Include(b => b.BorrowDetails)
                .FirstOrDefaultAsync(b => b.BorrowId == id);
            if (borrow == null) return NotFound();

            borrow.ReturnDate = DateTime.Now;
            borrow.Status = "Returned";

            var lateDays = (borrow.ReturnDate.Value - borrow.DueDate).Days;
            borrow.FineAmount = lateDays > 0 ? lateDays * 5000 : 0;

            _context.Update(borrow);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
