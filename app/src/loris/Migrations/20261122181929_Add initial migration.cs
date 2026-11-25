using System;
using Loris.Models;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class Addinitialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:device_status", "active,disconnected,ignored,pending")
                .Annotation("Npgsql:Enum:func_arg_type", "bool,float,int,string,unspecified,void")
                .Annotation("Npgsql:Enum:sensor_semantics", "aqi,area,battery,co2,current,data_rate,data_size,distance,duration,energy,frequency,humidity,illuminance,moisture,nitrogen,ph,phosphorus,pm1,pm10,pm25,pm4,potassium,pressure,signal_strength,temperature,universal,unspecified,uptime,voltage,water,weight,wind_speed")
                .Annotation("Npgsql:Enum:sensor_value_type", "bool,float,int,text,unspecified");

            migrationBuilder.CreateTable(
                name: "automations",
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
                    table.PrimaryKey("pk_automations", x => x.automation_id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dev_eui = table.Column<byte[]>(type: "bytea", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: false),
                    friendly_name = table.Column<string>(type: "text", nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    device_status = table.Column<DeviceStatus>(type: "device_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.device_id);
                },
                comment: "All of the devices from Chirpstack");

            migrationBuilder.CreateTable(
                name: "users",
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
                columns: table => new
                {
                    function_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_function_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    arg_type = table.Column<FuncArgType>(type: "func_arg_type", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_functions", x => x.function_id);
                    table.ForeignKey(
                        name: "fk_functions_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "device_id");
                },
                comment: "Devices functions exposed via capabilities ");

            migrationBuilder.CreateTable(
                name: "sensors",
                columns: table => new
                {
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_sensor_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: true),
                    value_type = table.Column<SensorValueType>(type: "sensor_value_type", nullable: false),
                    semantics = table.Column<SensorSemantics>(type: "sensor_semantics", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.sensor_id);
                    table.ForeignKey(
                        name: "fk_sensors_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "device_id");
                },
                comment: "Devices sensors exposed via capabilities");

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                columns: table => new
                {
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    int_value = table.Column<int>(type: "integer", nullable: true),
                    float_value = table.Column<float>(type: "real", nullable: true),
                    string_value = table.Column<string>(type: "text", nullable: true),
                    bool_value = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_readings", x => new { x.sensor_id, x.time });
                    table.ForeignKey(
                        name: "fk_sensor_readings_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "sensor_id");
                },
                comment: "Readings of actuals sensors exposed via capabilities");

            migrationBuilder.CreateIndex(
                name: "ix_devices_dev_eui",
                table: "devices",
                column: "dev_eui",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_friendly_name",
                table: "devices",
                column: "friendly_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_functions_device_id",
                table: "functions",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_device_id",
                table: "sensors",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_display_name",
                table: "users",
                column: "display_name",
                unique: true);

            migrationBuilder.CreateHypertable(
                tableName: "sensor_readings",
                timeColumnName: "time",
                schema: "public",
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
                name: "automations");

            migrationBuilder.DropTable(
                name: "functions");

            migrationBuilder.DropTable(
                name: "sensor_readings");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "sensors");

            migrationBuilder.DropTable(
                name: "devices");
        }
    }
}
