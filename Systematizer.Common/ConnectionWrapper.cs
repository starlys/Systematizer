using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace Systematizer.Common
{
    /// <summary>
    /// Encapsulates single global connection to the database file and closing it when idle.
    /// NotifyIdle must be called from UI repeatedly at idle times.
    /// </summary>
    public class ConnectionWrapper : IDisposable
    {
        DateTime LastUsedUtc = DateTime.UtcNow;
        DbConnection OpenInstance;

        public void NotifyIdle()
        {
            lock (this)
            {
                if (OpenInstance != null && LastUsedUtc.AddMinutes(3) < DateTime.UtcNow)
                    Dispose();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                OpenInstance?.Dispose();
                OpenInstance = null;
            }
        }

        public DbConnection Get()
        {
            lock (this)
            {
                LastUsedUtc = DateTime.UtcNow;
                if (OpenInstance == null)
                    OpenInstance = new SqliteConnection($"Data Source={Globals.DatabasePath};Mode=ReadWrite;");
                return OpenInstance;
            }
        }
    }
}
