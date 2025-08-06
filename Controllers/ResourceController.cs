using Microsoft.AspNetCore.Mvc;
using WarehouseApp.Data;
using WarehouseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace WarehouseApp.Controllers
{
    public class ResourceController : Controller
    {
        private readonly AppDbContext _context;
        public ResourceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Resource/Index – список ресурсов
        public async Task<IActionResult> Index()
        {
            var resources = await _context.Resources.ToListAsync();
            return View(resources);
        }

        // GET: – форма создания нового ресурса
        public IActionResult Create()
        {
            return View();
        }

        // POST: – сохранение нового ресурса
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Resource resource)
        {
            if (ModelState.IsValid)
            {
                // Проверка уникальности наименования (без учёта регистра)
                bool exists = await _context.Resources
                    .AnyAsync(r => r.Name.ToLower() == resource.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Ресурс с таким наименованием уже существует.");
                }
                else
                {
                    _context.Add(resource);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(resource);
        }

        // GET: – форма редактирования ресурса
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
                return NotFound();
            return View(resource);
        }

        // POST: – сохранение изменений ресурса
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsArchived")] Resource resource)
        {
            if (id != resource.Id)
                return NotFound();
            if (ModelState.IsValid)
            {
                // Проверка уникальности имени (исключая данный ресурс)
                bool exists = await _context.Resources
                    .AnyAsync(r => r.Id != resource.Id && r.Name.ToLower() == resource.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Ресурс с таким наименованием уже существует.");
                    return View(resource);
                }
                try
                {
                    _context.Update(resource);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Resources.Any(e => e.Id == resource.Id))
                        return NotFound();
                    throw;
                }
            }
            return View(resource);
        }

        // POST: – удаление ресурса
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                // Проверка использования ресурса в документах
                bool inUse = await _context.ReceiptItems.AnyAsync(i => i.ResourceId == id);
                if (inUse)
                {
                    // Нельзя удалить – запись об ошибке во временное хранилище
                    TempData["Error"] = "Невозможно удалить ресурс, так как он используется в документах. Архивируйте его.";
                }
                else
                {
                    _context.Resources.Remove(resource);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
