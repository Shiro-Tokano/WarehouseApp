namespace WarehouseApp.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Name), IsUnique = true)]
    public class Unit                         // еденица измерения
    {
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;
        public bool IsArchived { get; set; } = false;
    }
}
