namespace WarehouseApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Number), IsUnique = true)]
    public class ReceiptDocument // документ поступления
    {
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Number { get; set; } = null!;
        public DateOnly Date { get; set; }
        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    }
}
