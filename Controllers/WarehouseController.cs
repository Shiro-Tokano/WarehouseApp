using Microsoft.AspNetCore.Mvc;
using WarehouseApp.Data;
using WarehouseApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace WarehouseApp.Controllers
{
    public class WarehouseController : Controller
    {
        private readonly AppDbContext _context;
        public WarehouseController(AppDbContext context)
        {
            _context = context;
        }

        // GET: – список всех строк поступления (ресурсов) с фильтрацией
        public async Task<IActionResult> Index(List<int>? selectedResourceIds, List<int>? selectedUnitIds)
        {
            var query = _context.ReceiptItems
                .Include(i => i.Resource)
                .Include(i => i.Unit)
                .Include(i => i.ReceiptDocument)
                .AsQueryable();
            // Фильтр по выбранным ресурсам
            if (selectedResourceIds != null && selectedResourceIds.Count > 0)
            {
                query = query.Where(i => selectedResourceIds.Contains(i.ResourceId));
            }
            // Фильтр по выбранным единицам
            if (selectedUnitIds != null && selectedUnitIds.Count > 0)
            {
                query = query.Where(i => selectedUnitIds.Contains(i.UnitId));
            }
            var items = await query.ToListAsync();
            // Формируем списки значений для фильтров (все ресурсы и единицы)
            ViewBag.ResourcesList = new SelectList(_context.Resources.OrderBy(r => r.Name).ToList(), "Id", "Name");
            ViewBag.UnitsList = new SelectList(_context.Units.OrderBy(u => u.Name).ToList(), "Id", "Name");
            ViewBag.SelectedResourceIds = selectedResourceIds ?? new List<int>();
            ViewBag.SelectedUnitIds = selectedUnitIds ?? new List<int>();
            return View(items);
        }
    }
}
