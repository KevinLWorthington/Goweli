using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Diagnostics;

namespace Goweli.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }

        private string _dbPath;

        public AppDbContext()
        {
            // Use a more predictable path - storing in the application's root directory
            var appRoot = AppDomain.CurrentDomain.BaseDirectory;
            // Go up from bin/Debug/net<version>
            var projectRoot = Path.GetFullPath(Path.Combine(appRoot, "../../.."));
            _dbPath = Path.Combine(projectRoot, "books.db");

            // Log the path we're using
            Debug.WriteLine($"Database path: {_dbPath}");

            // Check if the file exists
            if (File.Exists(_dbPath))
            {
                Debug.WriteLine("Database file exists");
            }
            else
            {
                Debug.WriteLine("Database file does not exist at this location");
            }

            /* public DbSet<Book> Books { get; set; }

             private string _dbPath;

             public AppDbContext()
             {
                 // Define the path to the SQLite database file
                 var folder = Environment.CurrentDirectory;
                 _dbPath = System.IO.Path.Combine(folder, "books.db");

                 // Ensure the database is created
                 //Database.EnsureCreated();
             }

             protected override void OnConfiguring(DbContextOptionsBuilder options)
                 => options.UseSqlite($"Data Source={_dbPath}"); */
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_dbPath}");

            // Enable detailed error messages and logging
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();

            // Log the connection string
            Debug.WriteLine($"Connection string: Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly configure the Book entity
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BookTitle).IsRequired();
                entity.Property(e => e.AuthorName).IsRequired();
                entity.Property(e => e.CoverUrl).IsRequired(false);
                entity.Property(e => e.ISBN).IsRequired(false);
                entity.Property(e => e.Synopsis).IsRequired(false);
            });
        }
    }
}