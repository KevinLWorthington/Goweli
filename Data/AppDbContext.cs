using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Goweli.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IJSRuntime _jsRuntime;
        private static bool _databaseInitialized = false;
        private const string DatabaseName = "goweli_books.db";

        public DbSet<Book> Books { get; set; }

        public AppDbContext()
        {
            
        }

        public AppDbContext(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

            if (!_databaseInitialized)
            {
                InitializeDatabaseAsync().GetAwaiter().GetResult();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DatabaseName}");

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("initSqliteWasm");

                await _jsRuntime.InvokeVoidAsync("createDatabase", DatabaseName);

                await _jsRuntime.InvokeVoidAsync("executeNonQuery", DatabaseName,
                    "CREATE TABLE IF NOT EXISTS Books (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "BookTitle TEXT NOT NULL," +
                    "AuthorName TEXT NOT NULL," +
                    "ISBN TEXT," +
                    "Synopsis TEXT," +
                    "IsChecked BOOLEAN," +
                    "CoverUrl TEXT)");

                _databaseInitialized = true;
                Console.WriteLine("SQLite WASM database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SQLite WASM database: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        public async Task<T[]> ExecuteQueryAsync<T>(string sql, Func<dynamic, T> mapper)
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<dynamic[]>("executeQuery", DatabaseName, sql);

                T[] typedResult = new T[result.Length];

                for (int i = 0; i < result.Length; i++)
                {
                    typedResult[i] = mapper(result[i]);
                }

                return typedResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                return Array.Empty<T>();
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<int>("executeNonQuery", DatabaseName, sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing non-query: {ex.Message}");
                return -1;
            }
        }
    }
}