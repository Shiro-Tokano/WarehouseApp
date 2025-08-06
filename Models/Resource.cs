namespace WarehouseApp.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Name), IsUnique = true)]
    public class Resource            // ресурс
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;
        public bool IsArchived { get; set; } = false;
    }
}
