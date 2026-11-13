using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class changesensorreadingindexorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_sensor_readings",
                table: "sensor_readings");

            migrationBuilder.DropIndex(
                name: "ix_sensor_readings_dev_eui_sensor_id",
                table: "sensor_readings");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sensor_readings",
                table: "sensor_readings",
                columns: new[] { "dev_eui", "sensor_id", "time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_sensor_readings",
                table: "sensor_readings");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sensor_readings",
                table: "sensor_readings",
                columns: new[] { "sensor_id", "dev_eui", "time" });

            migrationBuilder.CreateIndex(
                name: "ix_sensor_readings_dev_eui_sensor_id",
                table: "sensor_readings",
                columns: new[] { "dev_eui", "sensor_id" });
        }
    }
}
