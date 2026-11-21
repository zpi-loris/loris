using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class Addautomationentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automation",
                columns: table => new
                {
                    automation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    triggers = table.Column<string>(type: "jsonb", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_automation", x => x.automation_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automation");
        }
    }
}
