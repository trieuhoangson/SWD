using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Models;
using System.Linq;
using System.Threading.Tasks;
using SWD.Models;

namespace SWD
{
    public class CategoriesController : Controller
    {
        private readonly LibraryManagementSystemContext _context;

        public CategoriesController(LibraryManagementSystemContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var categories = _context.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                categories = categories.Where(c => c.CatName.Contains(search));

            return View(await categories.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CatId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
