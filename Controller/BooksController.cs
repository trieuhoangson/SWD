using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace SWD.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public BooksController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index(string search, string sort)
        {
            var books = from b in _context.Books.Include(b => b.Cat)
                        select b;

            if (!string.IsNullOrEmpty(search))
                books = books.Where(b => b.Title.Contains(search) || b.Author.Contains(search));

            books = sort switch
            {
                "title_desc" => books.OrderByDescending(b => b.Title),
                "year" => books.OrderBy(b => b.PublicationYear),
                _ => books.OrderBy(b => b.Title)
            };
            ViewData["Search"] = search;
            ViewData["Sort"] = sort;
            ViewData["Title"] = "Book List";
            return View(await books.ToListAsync());
        }

        // GET: Books/Borrow/5
        [HttpGet]
        public async Task<IActionResult> Borrow(int id)
        {
            // Require login
            if (User?.Identity?.IsAuthenticated != true)
            {
                var returnUrl = Url.Action("Borrow", "Books", new { id });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            // Get current user id
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                TempData["Error"] = "Không xác định được người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Index));
            }

            // Validate library card
            var now = DateTime.Now;
            var card = await _context.LibraryCards
                .AsNoTracking()
                .Where(c => c.UserId == userId && (c.Status == null || c.Status == "Active"))
                .OrderByDescending(c => c.ExpiryDate)
                .FirstOrDefaultAsync();

            if (card == null)
            {
                TempData["Error"] = "Bạn chưa có thẻ thư viện nên không thể mượn sách.";
                return RedirectToAction(nameof(Index));
            }

            if (card.ExpiryDate < now)
            {
                TempData["Error"] = "Thẻ thư viện của bạn đã hết hạn. Vui lòng gia hạn để mượn sách.";
                return RedirectToAction(nameof(Index));
            }

            // Basic requirement met
            TempData["Success"] = "Bạn đủ điều kiện mượn sách: có thẻ hợp lệ và chưa hết hạn.";
            return RedirectToAction(nameof(Index));
        }

        // AJAX: validate and return book info for modal
        [HttpGet]
        public async Task<IActionResult> BorrowCheck(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Json(new { ok = false, msg = "Vui lòng đăng nhập." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Json(new { ok = false, msg = "Không xác định được người dùng." });

            var now = DateTime.Now;
            var card = await _context.LibraryCards
                .AsNoTracking()
                .Where(c => c.UserId == userId && (c.Status == null || c.Status == "Active"))
                .OrderByDescending(c => c.ExpiryDate)
                .FirstOrDefaultAsync();
            if (card == null)
                return Json(new { ok = false, msg = "Bạn chưa có thẻ thư viện nên không thể mượn sách." });
            if (card.ExpiryDate < now)
                return Json(new { ok = false, msg = "Thẻ thư viện của bạn đã hết hạn." });

            var book = await _context.Books.Include(b => b.Cat).FirstOrDefaultAsync(b => b.BookId == id);
            if (book == null)
                return Json(new { ok = false, msg = "Sách không tồn tại." });

            return Json(new
            {
                ok = true,
                book = new
                {
                    id = book.BookId,
                    title = book.Title,
                    author = book.Author,
                    publisher = book.Publisher,
                    year = book.PublicationYear,
                    category = book.Cat?.CatName
                }
            });
        }

        // AJAX: create borrow request with Processing status
        [HttpPost]
        public async Task<IActionResult> RequestBorrow(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Json(new { ok = false, msg = "Vui lòng đăng nhập." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Json(new { ok = false, msg = "Không xác định được người dùng." });

            var now = DateTime.Now;
            var cardOk = await _context.LibraryCards.AnyAsync(c => c.UserId == userId && (c.Status == null || c.Status == "Active") && c.ExpiryDate >= now);
            if (!cardOk)
                return Json(new { ok = false, msg = "Không đủ điều kiện mượn (thẻ không hợp lệ/hết hạn)." });

            var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == id);
            if (book == null)
                return Json(new { ok = false, msg = "Sách không tồn tại." });

            var borrow = new BorrowTransaction
            {
                UserId = userId,
                BorrowDate = now,
                DueDate = now.AddDays(14),
                Status = "Processing"
            };
            borrow.BorrowDetails.Add(new BorrowDetail { BookId = book.BookId, Quantity = 1 });

            _context.BorrowTransactions.Add(borrow);
            await _context.SaveChangesAsync();

            return Json(new { ok = true, redirect = Url.Action("Index", "MyLoans", new { userId }) });
        }

        [HttpPost]
        public async Task<IActionResult> Borrow(int? bookid)
        {

            return View();
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Cat)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null) return NotFound();

            ViewData["Title"] = "Book Details";
            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewData["CatId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "CatId", "CatName");
            ViewData["Title"] = "Create Book";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CatId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "CatId", "CatName", book.CatId);
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            ViewData["CatId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "CatId", "CatName", book.CatId);
            ViewData["Title"] = "Edit Book";
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.BookId == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CatId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "CatId", "CatName", book.CatId);
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Cat)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null) return NotFound();

            ViewData["Title"] = "Delete Book";
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
