using Microservice.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Microservice.Database
{
    public class DatabaseContext : DbContext
    {
        public static string ConnectionString { get; set; }

        // Database Entities\Tables
        public DbSet<Request> Requests { get; set; }


        public DatabaseContext(string connectionString = null)
        {
            if (!string.IsNullOrEmpty(connectionString))
                ConnectionString = connectionString;

            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            //optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
#endif
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ADDITIONAL INDEXES
            //modelBuilder.Entity<[TargetEntity]>().HasIndex(b => b.[TargetField]);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "p");
                    var filter = Expression.Lambda(
                        Expression.Not(
                            Expression.Property(parameter, isDeletedProperty.PropertyInfo)),
                        parameter);
                    entityType.SetQueryFilter(filter);
                }
            }
        }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.CurrentValues["IsDeleted"] = true;
                }
            }

            var entries = ChangeTracker
              .Entries()
              .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));
            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).LastModifiedAt = DateTime.UtcNow;
                if (entityEntry.State == EntityState.Added)
                    ((BaseEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            return base.SaveChanges();
        }

        public class DataContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
        {
            private readonly IConfiguration _configuration;

            public DataContextFactory(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public DataContextFactory()
            {
            }

            public DatabaseContext CreateDbContext(string[] args)
            {
                if (_configuration == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                    var config = builder.Build();
                    ConnectionString = config.GetConnectionString("DefaultConnection");
                }
                else
                    ConnectionString = _configuration.GetConnectionString("DefaultConnection");

                return new DatabaseContext(ConnectionString);
            }

        }
    }

}