using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class Initialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "loris_schema");

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "loris_schema",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    dev_eui = table.Column<byte[]>(type: "bytea", nullable: false),
                    device_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    friendly_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    device_status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.device_id);
                    table.UniqueConstraint("ak_devices_dev_eui", x => x.dev_eui);
                },
                comment: "All of the devices from Chirpstack");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "loris_schema",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                },
                comment: "Web client users");

            migrationBuilder.CreateTable(
                name: "functions",
                schema: "loris_schema",
                columns: table => new
                {
                    dev_eui = table.Column<byte[]>(type: "bytea", nullable: false),
                    function_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_functions", x => new { x.dev_eui, x.function_id });
                    table.ForeignKey(
                        name: "fk_functions_devices_dev_eui",
                        column: x => x.dev_eui,
                        principalSchema: "loris_schema",
                        principalTable: "devices",
                        principalColumn: "dev_eui");
                },
                comment: "Devices functions exposed via capabilities ");

            migrationBuilder.CreateTable(
                name: "sensors",
                schema: "loris_schema",
                columns: table => new
                {
                    dev_eui = table.Column<byte[]>(type: "bytea", nullable: false),
                    sensor_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    unit = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => new { x.dev_eui, x.sensor_id });
                    table.ForeignKey(
                        name: "fk_sensors_devices_dev_eui",
                        column: x => x.dev_eui,
                        principalSchema: "loris_schema",
                        principalTable: "devices",
                        principalColumn: "dev_eui");
                },
                comment: "Devices sensors exposed via capabilities");

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                schema: "loris_schema",
                columns: table => new
                {
                    dev_eui = table.Column<byte[]>(type: "bytea", nullable: false),
                    sensor_id = table.Column<int>(type: "integer", nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    int_value = table.Column<int>(type: "integer", nullable: true),
                    float_value = table.Column<float>(type: "real", nullable: true),
                    string_value = table.Column<string>(type: "text", nullable: true),
                    bool_value = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_readings", x => new { x.sensor_id, x.dev_eui, x.time });
                    table.ForeignKey(
                        name: "fk_sensor_readings_sensors_dev_eui_sensor_id",
                        columns: x => new { x.dev_eui, x.sensor_id },
                        principalSchema: "loris_schema",
                        principalTable: "sensors",
                        principalColumns: new[] { "dev_eui", "sensor_id" },
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Readings of actuals sensors exposed via capabilities");

            migrationBuilder.CreateIndex(
                name: "ix_devices_dev_eui",
                schema: "loris_schema",
                table: "devices",
                column: "dev_eui",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_friendly_name",
                schema: "loris_schema",
                table: "devices",
                column: "friendly_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensor_readings_dev_eui_sensor_id",
                schema: "loris_schema",
                table: "sensor_readings",
                columns: new[] { "dev_eui", "sensor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_display_name",
                schema: "loris_schema",
                table: "users",
                column: "display_name",
                unique: true);

            migrationBuilder.CreateHypertable(
                tableName: "sensor_readings",
                timeColumnName: "time",
                schema: "loris_schema",
                chunkTimeInterval: "1 day",
                enableCompression: true,
                compressionSegmentBy: ["sensor_id"],
                compressionOrderBy: ["time DESC"],
                compressChunkTimeInterval: "7 days");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "functions",
                schema: "loris_schema");

            migrationBuilder.DropTable(
                name: "sensor_readings",
                schema: "loris_schema");

            migrationBuilder.DropTable(
                name: "users",
                schema: "loris_schema");

            migrationBuilder.DropTable(
                name: "sensors",
                schema: "loris_schema");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "loris_schema");
        }
    }
}
