using Microsoft.AspNetCore.Mvc;
using WarehouseApp.Data;
using WarehouseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace WarehouseApp.Controllers
{
    public class UnitController : Controller
    {
        private readonly AppDbContext _context;
        public UnitController(AppDbContext context)
        {
            _context = context;
        }

        // GET: – список единиц измерения
        public async Task<IActionResult> Index()
        {
            var units = await _context.Units.ToListAsync();
            return View(units);
        }

        // GET: – форма создания новой единицы
        public IActionResult Create()
        {
            return View();
        }

        // POST: – сохранение новой единицы измерения
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Unit unit)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.Units
                    .AnyAsync(u => u.Name.ToLower() == unit.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Единица измерения с таким наименованием уже существует.");
                }
                else
                {
                    _context.Add(unit);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(unit);
        }

        // GET: – форма редактирования единицы измерения
        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        // POST: – сохранение изменений единицы измерения
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsArchived")] Unit unit)
        {
            if (id != unit.Id) return NotFound();
            if (ModelState.IsValid)
            {
                bool exists = await _context.Units
                    .AnyAsync(u => u.Id != unit.Id && u.Name.ToLower() == unit.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Единица измерения с таким наименованием уже существует.");
                    return View(unit);
                }
                try
                {
                    _context.Update(unit);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Units.Any(e => e.Id == unit.Id))
                        return NotFound();
                    throw;
                }
            }
            return View(unit);
        }

        // POST: – удаление единицы измерения
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                bool inUse = await _context.ReceiptItems.AnyAsync(i => i.UnitId == id);
                if (inUse)
                {
                    TempData["Error"] = "Невозможно удалить единицу измерения, так как она используется. Переведите её в архив.";
                }
                else
                {
                    _context.Units.Remove(unit);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
