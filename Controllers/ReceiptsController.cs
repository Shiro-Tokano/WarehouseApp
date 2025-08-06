using Microsoft.AspNetCore.Mvc;
using WarehouseApp.Data;
using WarehouseApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Controllers
{
    public class ReceiptsController : Controller
    {
        private readonly AppDbContext _context;
        public ReceiptsController(AppDbContext context)
        {
            _context = context;
        }

        // список документов поступления с фильтрацией
        public async Task<IActionResult> Index(DateOnly? dateFrom, DateOnly? dateTo,
            List<int>? selectedResourceIds, List<int>? selectedUnitIds, List<string>? selectedDocNumbers)
        {
            // Формирование запроса: включаем связанные строки, ресурсы и единицы 
            var query = _context.ReceiptDocuments
                .Include(d => d.Items).ThenInclude(item => item.Resource)
                .Include(d => d.Items).ThenInclude(item => item.Unit)
                .AsQueryable();

            // Применение фильтров по дате
            if (dateFrom.HasValue)
                query = query.Where(d => d.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(d => d.Date <= dateTo.Value);
            // Фильтр по выбранным ресурсам (документы, содержащие хотя бы один из ресурсов)
            if (selectedResourceIds != null && selectedResourceIds.Count > 0)
            {
                query = query.Where(d => d.Items.Any(item => selectedResourceIds.Contains(item.ResourceId)));
            }
            // Фильтр по выбранным единицам измерения
            if (selectedUnitIds != null && selectedUnitIds.Count > 0)
            {
                query = query.Where(d => d.Items.Any(item => selectedUnitIds.Contains(item.UnitId)));
            }
            // Фильтр по выбранным номерам документов
            if (selectedDocNumbers != null && selectedDocNumbers.Count > 0)
            {
                query = query.Where(d => selectedDocNumbers.Contains(d.Number));
            }

            // Выполнение запроса и получение списка отфильтрованных документов
            var receipts = await query.ToListAsync();

            // Заполнение списков значений для фильтров (все варианты из базы, без учёта периода)
            ViewBag.ResourcesList = new MultiSelectList(_context.Resources.OrderBy(r => r.Name).ToList(), "Id", "Name", selectedResourceIds);
            ViewBag.UnitsList = new MultiSelectList(_context.Units.OrderBy(u => u.Name).ToList(), "Id", "Name", selectedUnitIds);
            ViewBag.DocNumbersList = new MultiSelectList(_context.ReceiptDocuments.OrderBy(d => d.Number).Select(d => d.Number).Distinct().ToList(), selectedDocNumbers);
            // Сохранение текущих выбранных значений (для отметки в интерфейсе)
            ViewBag.SelectedResourceIds = selectedResourceIds ?? new List<int>();
            ViewBag.SelectedUnitIds = selectedUnitIds ?? new List<int>();
            ViewBag.SelectedDocNumbers = selectedDocNumbers ?? new List<string>();
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            return View(receipts);
        }

        // форма создания нового документа поступления
        public IActionResult Create()
        {
            // Создаём модель представления с текущей датой и одной пустой строкой для ввода
            var model = new ReceiptEditViewModel
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<ReceiptItemViewModel> { new ReceiptItemViewModel() }
            };
            // Справочники для выпадающих списков: только активные ресурсы и единицы
            ViewBag.ResourcesList = new SelectList(_context.Resources.Where(r => !r.IsArchived).OrderBy(r => r.Name).ToList(), "Id", "Name");
            ViewBag.UnitsList = new SelectList(_context.Units.Where(u => !u.IsArchived).OrderBy(u => u.Name).ToList(), "Id", "Name");
            return View(model);
        }

        // сохранение нового документа поступления
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceiptEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Проверка уникальности номера документа
                if (string.IsNullOrWhiteSpace(model.Number))
                {
                    ModelState.AddModelError("Number", "Укажите номер документа.");
                }
                else
                {
                    bool numberExists = await _context.ReceiptDocuments
                        .AnyAsync(d => d.Number.ToLower() == model.Number!.ToLower());
                    if (numberExists)
                    {
                        ModelState.AddModelError("Number", "Документ с таким номером уже существует.");
                    }
                }

                // Валидация заполнения строк поступления
                var itemsToAdd = new List<ReceiptItem>();
                foreach (var item in model.Items)
                {
                    bool filled = item.ResourceId != 0 || item.UnitId != 0 || item.Quantity != 0;
                    bool fullyFilled = item.ResourceId != 0 && item.UnitId != 0 && item.Quantity > 0;
                    if (filled)
                    {
                        if (!fullyFilled)
                        {
                            ModelState.AddModelError("", "Имеется незаполненная строка ресурса. Удалите её или заполните полностью.");
                            break;
                        }
                        // Подготовка добавления заполненной строки
                        itemsToAdd.Add(new ReceiptItem { ResourceId = item.ResourceId, UnitId = item.UnitId, Quantity = item.Quantity });
                    }
                }
                if (ModelState.IsValid)
                {
                    // Создание и сохранение документа с валидными строками
                    var doc = new ReceiptDocument { Number = model.Number, Date = model.Date };
                    foreach (var item in itemsToAdd)
                    {
                        doc.Items.Add(item);
                    }
                    _context.ReceiptDocuments.Add(doc);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            // Повторная загрузка списков (на случай ошибки валидации)
            ViewBag.ResourcesList = new SelectList(_context.Resources.Where(r => !r.IsArchived).OrderBy(r => r.Name).ToList(), "Id", "Name");
            ViewBag.UnitsList = new SelectList(_context.Units.Where(u => !u.IsArchived).OrderBy(u => u.Name).ToList(), "Id", "Name");
            return View(model);
        }

        // форма редактирования документа поступления
        public async Task<IActionResult> Edit(int id)
        {
            var doc = await _context.ReceiptDocuments
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return NotFound();
            // Формируем модель представления на основе данных документа
            var model = new ReceiptEditViewModel
            {
                Id = doc.Id,
                Number = doc.Number,
                Date = doc.Date,
                Items = (doc.Items.Any()
                    ? doc.Items.Select(item => new ReceiptItemViewModel
                    {
                        Id = item.Id,
                        ResourceId = item.ResourceId,
                        UnitId = item.UnitId,
                        Quantity = item.Quantity
                    }).ToList()
                    : new List<ReceiptItemViewModel> { new ReceiptItemViewModel() })
            };
            // Формируем списки ресурсов и единиц: активные + те архивные, что используются в данном документе
            var usedResourceIds = model.Items.Select(i => i.ResourceId).ToList();
            var usedUnitIds = model.Items.Select(i => i.UnitId).ToList();
            var resourcesQuery = _context.Resources.Where(r => !r.IsArchived || usedResourceIds.Contains(r.Id));
            var unitsQuery = _context.Units.Where(u => !u.IsArchived || usedUnitIds.Contains(u.Id));
            ViewBag.ResourcesList = new SelectList(resourcesQuery.OrderBy(r => r.Name).ToList(), "Id", "Name");
            ViewBag.UnitsList = new SelectList(unitsQuery.OrderBy(u => u.Name).ToList(), "Id", "Name");
            return View(model);
        }

        // сохранение изменений документа поступления
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReceiptEditViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (ModelState.IsValid)
            {
                // Проверка уникальности номера (исключая данный документ)
                if (string.IsNullOrWhiteSpace(model.Number))
                {
                    ModelState.AddModelError("Number", "Укажите номер документа.");
                }
                else
                {
                    bool numberExists = await _context.ReceiptDocuments
                        .AnyAsync(d => d.Id != model.Id && d.Number.ToLower() == model.Number!.ToLower());
                    if (numberExists)
                    {
                        ModelState.AddModelError("Number", "Документ с таким номером уже существует.");
                    }
                }
                // Проверка заполнения строк
                var itemsToSave = new List<ReceiptItem>();
                foreach (var item in model.Items)
                {
                    bool filled = item.ResourceId != 0 || item.UnitId != 0 || item.Quantity != 0;
                    bool fullyFilled = item.ResourceId != 0 && item.UnitId != 0 && item.Quantity > 0;
                    if (filled)
                    {
                        if (!fullyFilled)
                        {
                            ModelState.AddModelError("", "Имеется незаполненная строка ресурса. Удалите её или заполните полностью.");
                            break;
                        }
                        itemsToSave.Add(new ReceiptItem { ResourceId = item.ResourceId, UnitId = item.UnitId, Quantity = item.Quantity });
                    }
                }
                if (ModelState.IsValid)
                {
                    // Сохранение изменений документа
                    var doc = await _context.ReceiptDocuments
                        .Include(d => d.Items)
                        .FirstOrDefaultAsync(d => d.Id == model.Id);
                    if (doc == null) return NotFound();
                    // Обновляем поля документа
                    doc.Number = model.Number;
                    doc.Date = model.Date;
                    // Удаляем старые строки и добавляем заново актуальные
                    _context.ReceiptItems.RemoveRange(doc.Items);
                    foreach (var item in itemsToSave)
                    {
                        doc.Items.Add(item);
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            // В случае ошибки – заново формируем выпадающие списки (с учётом архивных из модели)
            var usedResourceIds = model.Items.Select(i => i.ResourceId).ToList();
            var usedUnitIds = model.Items.Select(i => i.UnitId).ToList();
            var resourcesQuery = _context.Resources.Where(r => !r.IsArchived || usedResourceIds.Contains(r.Id));
            var unitsQuery = _context.Units.Where(u => !u.IsArchived || usedUnitIds.Contains(u.Id));
            ViewBag.ResourcesList = new SelectList(resourcesQuery.OrderBy(r => r.Name).ToList(), "Id", "Name");
            ViewBag.UnitsList = new SelectList(unitsQuery.OrderBy(u => u.Name).ToList(), "Id", "Name");
            return View(model);
        }

        // удаление документа поступления
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.ReceiptDocuments.FindAsync(id);
            if (doc != null)
            {
                _context.ReceiptDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    // Вспомогательные модели представления для форм документа поступления:
    public class ReceiptItemViewModel
    {
        public int? Id { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        public decimal Quantity { get; set; }
    }
    public class ReceiptEditViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Укажите номер документа")]
        [MaxLength(50, ErrorMessage = "Максимальная длина номера — 50 символов")]
        public string Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите дату документа")]
        public DateOnly Date { get; set; }

        public List<ReceiptItemViewModel> Items { get; set; } = new List<ReceiptItemViewModel>();
    }
}
