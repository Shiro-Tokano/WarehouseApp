namespace WarehouseApp.Data
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseApp.Models;

    public class AppDbContext : DbContext
    {
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<ReceiptDocument> ReceiptDocuments => Set<ReceiptDocument>();
        public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связь: Документ поступления -> Ресурс поступления (каскадное удаление ресурсов поступления при удалении документа)
            modelBuilder.Entity<ReceiptItem>()
                .HasOne(i => i.ReceiptDocument)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.ReceiptDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            // Связь: Ресурс -> Ресурс поступления (запрет удаления ресурса, если есть ресурс поступления)
            modelBuilder.Entity<ReceiptItem>()
                .HasOne(i => i.Resource)
                .WithMany()
                .HasForeignKey(i => i.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);
            // Связь: Единица -> Ресурс поступления (запрет удаления единицы, если есть ресурс поступления)
            modelBuilder.Entity<ReceiptItem>()
                .HasOne(i => i.Unit)
                .WithMany()
                .HasForeignKey(i => i.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
