using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountFormulas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountFormula",
                table: "Transactions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmountUsedFormula",
                table: "AnnualBudgetItems",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotalAmountFormula",
                table: "AnnualBudgetItems",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountFormula",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AmountUsedFormula",
                table: "AnnualBudgetItems");

            migrationBuilder.DropColumn(
                name: "TotalAmountFormula",
                table: "AnnualBudgetItems");
        }
    }
}
