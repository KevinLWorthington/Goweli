using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Goweli.Data
{
    public class DbContextFactory : IDesignTimeDbContextFactory<GoweliDbContext>
    {
        // This method creates a new instance of the GoweliDbContext class
        public GoweliDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GoweliDbContext>();
            optionsBuilder.UseSqlite("Data Source=goweli.db");
            return new GoweliDbContext(optionsBuilder.Options);
        }
    }

    // GoweliDbContext class represents the database context
    public class GoweliDbContext : DbContext
    {
        public GoweliDbContext(DbContextOptions<GoweliDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }

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
