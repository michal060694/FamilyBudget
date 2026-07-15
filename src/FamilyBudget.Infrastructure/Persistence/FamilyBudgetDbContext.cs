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

    public DbSet<FixedDonationStandingOrder> FixedDonationStandingOrders => Set<FixedDonationStandingOrder>();

    public DbSet<Fund> Funds => Set<Fund>();

    public DbSet<FundEarmark> FundEarmarks => Set<FundEarmark>();

    public DbSet<Debt> Debts => Set<Debt>();

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

        modelBuilder.Entity<FixedDonationStandingOrder>(builder =>
        {
            builder.ToTable("FixedDonationStandingOrders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
            builder.Property(o => o.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.AmountFormula).HasMaxLength(200);
            builder.Property(o => o.ValidUntilYear);
            builder.Property(o => o.ValidUntilMonth);
        });

        modelBuilder.Entity<Fund>(builder =>
        {
            builder.ToTable("Funds");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
            builder.Property(f => f.TotalBalance).HasColumnType("decimal(18,2)").IsRequired();
        });

        modelBuilder.Entity<FundEarmark>(builder =>
        {
            builder.ToTable("FundEarmarks");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.PurposeLabel).IsRequired().HasMaxLength(200);
            builder.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();

            builder.HasOne<Fund>()
                .WithMany()
                .HasForeignKey(e => e.FundId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.FundId);
        });

        modelBuilder.Entity<Debt>(builder =>
        {
            builder.ToTable("Debts");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(d => d.CounterpartyName).IsRequired().HasMaxLength(200);
            builder.Property(d => d.OriginalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(d => d.CurrentBalance).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(d => d.TargetDate);
            builder.Property(d => d.RepaymentRate).HasColumnType("decimal(18,2)");
            builder.Property(d => d.Notes).HasMaxLength(1000);
        });
    }
}
