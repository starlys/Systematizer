using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Systematizer.Common.PersistentModel
{
    /// <summary>
    /// Context which reuses the same global connection for each instance
    /// </summary>
    class SystematizerContext : DbContext
    {
        public virtual DbSet<Box> Box { get; set; }
        public virtual DbSet<BoxPerson> BoxPerson { get; set; }
        public virtual DbSet<Cat> Cat { get; set; }
        public virtual DbSet<Person> Person { get; set; }
        public virtual DbSet<PersonCat> PersonCat { get; set; }
        public virtual DbSet<PersonPerson> PersonPerson { get; set; }
        public virtual DbSet<Setting> Setting { get; set; }
        public virtual DbSet<Word> Word { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Box>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.HasIndex(e => e.ParentId)
                    .HasName("Box_Parent");

                entity.Property(e => e.Title).IsRequired();
            });

            modelBuilder.Entity<BoxPerson>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.HasIndex(e => e.BoxId)
                    .HasName("BoxPerson_Box");

                entity.HasIndex(e => e.PersonId)
                    .HasName("BoxPerson_Person");
            });

            modelBuilder.Entity<Cat>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<PersonCat>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.HasIndex(e => e.CatId)
                    .HasName("PersonCat_Cat");

                entity.HasIndex(e => e.PersonId)
                    .HasName("PersonCat_Person");
            });

            modelBuilder.Entity<PersonPerson>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.HasIndex(e => e.Person1Id)
                    .HasName("PersonPerson_1");

                entity.HasIndex(e => e.Person2Id)
                    .HasName("PersonPerson_2");
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(e => e.RowId); 

                entity.Property(e => e.Custom1Label).IsRequired();
                entity.Property(e => e.Custom2Label).IsRequired();
                entity.Property(e => e.Custom3Label).IsRequired();
                entity.Property(e => e.Custom4Label).IsRequired();
                entity.Property(e => e.Custom5Label).IsRequired();
                entity.Property(e => e.AllowTasks).IsRequired();
            });

            modelBuilder.Entity<Word>(entity =>
            {
                entity.HasKey(e => e.RowId);

                entity.HasIndex(e => e.ParentId)
                    .HasName("Word_Parent");

                entity.HasIndex(e => e.Word8)
                    .HasName("Word_8");
            });

        }

        protected override void OnConfiguring(DbContextOptionsBuilder opt)
        {
            opt.UseSqlite(Globals.Connection.Get());

            //debug: uncomment next line
            //opt.UseLoggerFactory(new DBLoggerFactory());
        }
    }

    #region debug classes
    class DBLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DBLogger();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    class DBLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return new DBLoggerScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string msg = formatter(state, exception);
            //System.IO.File.AppendAllText(@"c:\temp\systematizer_log.txt", msg + "\r\n", encoding: System.Text.Encoding.UTF8);
        }
    }

    class DBLoggerScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
    #endregion
}
