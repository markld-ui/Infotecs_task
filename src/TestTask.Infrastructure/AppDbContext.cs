using Microsoft.EntityFrameworkCore;
using TestTask.Core.Entities;

namespace TestTask.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ValueRecord> Values => Set<ValueRecord>();
    public DbSet<ResultRecord> Results => Set<ResultRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValueRecord>(e =>
        {
            e.ToTable("Values");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(512);
            e.Property(x => x.Date).IsRequired();
            e.Property(x => x.ExecutionTime).IsRequired();
            e.Property(x => x.Value).IsRequired();

            // Поддерживает быстрое получение списка «10 последних файлов
            // (сортировка по имени и дате)» и удаление файла по имени при перезаписи.
            e.HasIndex(x => new { x.FileName, x.Date });
        });

        modelBuilder.Entity<ResultRecord>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(512);
            e.HasIndex(x => x.FileName).IsUnique();

            e.Property(x => x.DeltaTimeSeconds).IsRequired();
            e.Property(x => x.StartDate).IsRequired();
            e.Property(x => x.AverageExecutionTime).IsRequired();
            e.Property(x => x.AverageValue).IsRequired();
            e.Property(x => x.MedianValue).IsRequired();
            e.Property(x => x.MaxValue).IsRequired();
            e.Property(x => x.MinValue).IsRequired();
            e.Property(x => x.RowCount).IsRequired();
            e.Property(x => x.ProcessedAt).IsRequired();

            // Поддерживает фильтрацию по диапазону дат начала (StartDate)
            // и по диапазону средних значений или временному интервалу.
            e.HasIndex(x => x.StartDate);
            e.HasIndex(x => x.AverageValue);
            e.HasIndex(x => x.AverageExecutionTime);
        });
    }
}
