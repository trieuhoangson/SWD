using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using SWD.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SWD.Controllers
{
    /// <summary>
    /// BorrowingController - corresponds to BorrowingController in sequence diagram
    /// Route: /MyLoans (mapped via Route attribute to match diagram naming)
    /// </summary>
    [Route("MyLoans")]
    [Route("Borrowing")]
    public class BorrowingController : Controller
    {
        private readonly BorrowingService _borrowingService;
        private readonly LibraryManagementSystemContext _context;

        public BorrowingController(BorrowingService borrowingService, LibraryManagementSystemContext context)
        {
            _borrowingService = borrowingService;
            _context = context;
        }

        /// <summary>
        /// GET: MyLoans/Index or Borrowing/Index
        /// Corresponds to: accessMyLoans() -> loadBorrowedBooks() in sequence diagram
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? userId = null)
        {
            // Get current user ID and role
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            List<BorrowTransaction> borrowedList;

            // Check if user is Admin
            bool isAdmin = roleClaim == "Admin" || roleClaim == "Administrator";
            
            // If admin, show all borrow transactions (including pending requests from all members)
            if (isAdmin)
            {
                // Get all borrow transactions for admin view
                borrowedList = await _borrowingService.GetAllBorrowTransactions();
                ViewData["Title"] = "All Loans (Admin View)";
                ViewData["IsAdmin"] = true;
            }
            else
            {
                // For regular members, only show their own books
                // Use provided userId or current user's ID
                int targetUserId = userId ?? currentUserId;
                borrowedList = await _borrowingService.GetBorrowedBooks(targetUserId);
                ViewData["Title"] = "My Loans";
                ViewData["IsAdmin"] = false;
            }

            // Explicitly specify the view path since controller name is BorrowingController but view is in MyLoans folder
            return View("~/Views/MyLoans/Index.cshtml", borrowedList);
        }

        /// <summary>
        /// POST: MyLoans/RequestRenewal or Borrowing/RequestRenewal
        /// Corresponds to: selectRenewal(borrowId) -> requestRenewal(borrowId) -> processRenewal(borrowId) in sequence diagram
        /// </summary>
        [HttpPost("RequestRenewal")]
        public async Task<IActionResult> RequestRenewal(int borrowId)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new { ok = false, msg = "Please login to renew books." });
            }

            // Verify the borrow transaction belongs to current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Json(new { ok = false, msg = "User not identified." });
            }

            var borrow = await _context.BorrowTransactions.FirstOrDefaultAsync(b => b.BorrowId == borrowId);
            if (borrow == null || borrow.UserId != userId)
            {
                return Json(new { ok = false, msg = "Borrow transaction not found or access denied." });
            }

            // Process renewal through service
            // Corresponds to: processRenewal(borrowId) in sequence diagram
            var renewalStatus = await _borrowingService.ProcessRenewal(borrowId);

            if (renewalStatus.Success)
            {
                return Json(new
                {
                    ok = true,
                    msg = renewalStatus.Message,
                    newDueDate = renewalStatus.NewDueDate?.ToString("dd/MM/yyyy")
                });
            }
            else
            {
                return Json(new { ok = false, msg = renewalStatus.Message });
            }
        }

        // POST: MyLoans/Approve/{id} or Borrowing/Approve/{id} (admin approves -> Borrowing)
        [HttpPost("Approve/{id}")]
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
                var claimId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out var cid)) librarianId = cid;
                borrow.LibrarianId = librarianId;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: MyLoans/Reject/{id} or Borrowing/Reject/{id} (admin cancels request)
        [HttpPost("Reject/{id}")]
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

        // GET: MyLoans/Return/{id} or Borrowing/Return/{id}
        [HttpGet("Return/{id}")]
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

