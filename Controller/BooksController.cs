using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System.Linq;
using System.Threading.Tasks;

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

            ViewData["Title"] = "Book List"; // ✅ đảm bảo ViewData không null
            return View(await books.ToListAsync());
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
