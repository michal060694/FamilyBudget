using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyActionItemCompletedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CompletedDate",
                table: "MonthlyActionItems",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "MonthlyActionItems");
        }
    }
}
