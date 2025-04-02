using GoweliProxyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GoweliProxyApi.Data
{
    public class GoweliDbContext : DbContext
    {
        public GoweliDbContext(DbContextOptions<GoweliDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.BookTitle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ISBN).HasMaxLength(20);
                entity.Property(e => e.Synopsis).HasMaxLength(2000);
                entity.Property(e => e.CoverUrl).HasMaxLength(500);

                // Add any indexes
                entity.HasIndex(e => e.BookTitle);
                entity.HasIndex(e => e.AuthorName);
                entity.HasIndex(e => e.ISBN);
            });
        }
    }
}
