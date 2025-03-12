using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Goweli.Data
{
    public static class SQLiteInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public static bool Initialize(IJSRuntime jsRuntime)
        {
            if (_isInitialized)
            {
                return true;
            }

            lock (_lock)
            {
                if (_isInitialized)
                {
                    return true;
                }

                try
                {
                    Console.WriteLine("Initializing SQLite...");

                    jsRuntime.InvokeVoidAsync("console.log", "SQLite initialization requested");
                    _isInitialized = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing SQLite: {ex.Message}");
                    return false;
                }
            }
        }
    }
}