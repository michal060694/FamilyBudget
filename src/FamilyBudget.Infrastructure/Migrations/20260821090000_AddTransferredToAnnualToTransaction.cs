using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferredToAnnualToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TransferredToAnnual",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferredToAnnual",
                table: "Transactions");
        }
    }
}
