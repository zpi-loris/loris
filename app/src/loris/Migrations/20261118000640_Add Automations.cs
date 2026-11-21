using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_automation",
                table: "automation");

            migrationBuilder.RenameTable(
                name: "automation",
                newName: "automations");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automations",
                table: "automations",
                column: "automation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_automations",
                table: "automations");

            migrationBuilder.RenameTable(
                name: "automations",
                newName: "automation");

            migrationBuilder.AddPrimaryKey(
                name: "pk_automation",
                table: "automation",
                column: "automation_id");
        }
    }
}
