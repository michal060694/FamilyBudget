using FamilyBudget.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Persistence;

public class FamilyBudgetDbContext : DbContext
{
    public FamilyBudgetDbContext(DbContextOptions<FamilyBudgetDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnnualBudgetItem> AnnualBudgetItems => Set<AnnualBudgetItem>();

    public DbSet<AnnualReserve> AnnualReserves => Set<AnnualReserve>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnnualBudgetItem>(builder =>
        {
            builder.ToTable("AnnualBudgetItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Year).IsRequired();
            builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
            builder.Property(item => item.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(item => item.TargetMonth);
            builder.Property(item => item.AmountAlreadySetAside).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(item => item.AmountUsed).HasColumnType("decimal(18,2)").IsRequired();

            builder.HasIndex(item => item.Year);
        });

        modelBuilder.Entity<AnnualReserve>(builder =>
        {
            builder.ToTable("AnnualReserves");
            builder.HasKey(reserve => reserve.Year);
            builder.Property(reserve => reserve.Year).ValueGeneratedNever();
            builder.Property(reserve => reserve.Amount).HasColumnType("decimal(18,2)").IsRequired();
        });
    }
}
