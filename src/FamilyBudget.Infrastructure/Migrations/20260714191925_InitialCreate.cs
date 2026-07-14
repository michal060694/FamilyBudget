using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualBudgetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    AmountAlreadySetAside = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualBudgetItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnualReserves",
                columns: table => new
                {
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualReserves", x => x.Year);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualBudgetItems_Year",
                table: "AnnualBudgetItems",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnualBudgetItems");

            migrationBuilder.DropTable(
                name: "AnnualReserves");
        }
    }
}
