using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Goweli.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }

        private string _dbPath;

        public AppDbContext()
        {
            // Define the path to the SQLite database file
            var folder = Environment.CurrentDirectory;
            _dbPath = System.IO.Path.Combine(folder, "books.db");

            // Ensure the database is created
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={_dbPath}");
    }
}