using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyTemplateItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountFormula = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsTitheApplicable = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyTemplateItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyTemplateItems");
        }
    }
}
