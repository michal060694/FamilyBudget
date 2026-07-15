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

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TitheSetting> TitheSettings => Set<TitheSetting>();

    public DbSet<MonthlyExpenseBudgetItem> MonthlyExpenseBudgetItems => Set<MonthlyExpenseBudgetItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnnualBudgetItem>(builder =>
        {
            builder.ToTable("AnnualBudgetItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Year).IsRequired();
            builder.Property(item => item.Name).IsRequired().HasMaxLength(200);
            builder.Property(item => item.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(item => item.TotalAmountFormula).HasMaxLength(200);
            builder.Property(item => item.TargetMonth);
            builder.Property(item => item.AmountAlreadySetAside).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(item => item.AmountUsed).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(item => item.AmountUsedFormula).HasMaxLength(200);

            builder.HasIndex(item => item.Year);
        });

        modelBuilder.Entity<AnnualReserve>(builder =>
        {
            builder.ToTable("AnnualReserves");
            builder.HasKey(reserve => reserve.Year);
            builder.Property(reserve => reserve.Year).ValueGeneratedNever();
            builder.Property(reserve => reserve.Amount).HasColumnType("decimal(18,2)").IsRequired();
        });

        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Date).IsRequired();
            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(t => t.AmountFormula).HasMaxLength(200);
            builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(t => t.PaymentMethod).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(t => t.IsTitheApplicable);
            builder.Property(t => t.Description).HasMaxLength(500);

            builder.HasIndex(t => t.Date);
        });

        modelBuilder.Entity<TitheSetting>(builder =>
        {
            builder.ToTable("TitheSettings");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();
            builder.Property(t => t.Rate).HasColumnType("decimal(18,4)").IsRequired();
        });

        modelBuilder.Entity<MonthlyExpenseBudgetItem>(builder =>
        {
            builder.ToTable("MonthlyExpenseBudgetItems");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Year).IsRequired();
            builder.Property(i => i.Month).IsRequired();
            builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(i => i.BudgetedAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(i => i.BudgetedAmountFormula).HasMaxLength(200);
            builder.Property(i => i.UsedAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(i => i.UsedAmountFormula).HasMaxLength(200);

            builder.HasIndex(i => new { i.Year, i.Month });
        });
    }
}
