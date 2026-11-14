using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class Addmissingnameindexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_sensors_name",
                table: "sensors",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_functions_name",
                table: "functions",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sensors_name",
                table: "sensors");

            migrationBuilder.DropIndex(
                name: "ix_functions_name",
                table: "functions");
        }
    }
}
