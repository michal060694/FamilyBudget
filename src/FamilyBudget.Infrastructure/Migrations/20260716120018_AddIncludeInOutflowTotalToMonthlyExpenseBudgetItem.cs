using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncludeInOutflowTotalToMonthlyExpenseBudgetItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows predate this flag and were always counted toward TotalOutflow, so they
            // must default to true — a false default would silently drop them from the total.
            migrationBuilder.AddColumn<bool>(
                name: "IncludeInOutflowTotal",
                table: "MonthlyExpenseBudgetItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeInOutflowTotal",
                table: "MonthlyExpenseBudgetItems");
        }
    }
}
