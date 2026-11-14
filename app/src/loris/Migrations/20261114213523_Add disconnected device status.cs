using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loris.Migrations
{
    /// <inheritdoc />
    public partial class Adddisconnecteddevicestatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:device_status", "active,blocked,disconnected,pending")
                .Annotation("Npgsql:Enum:func_arg_type", "bool,float,int,string,unspecified,void")
                .Annotation("Npgsql:Enum:sensor_semantics", "aqi,area,battery,co2,current,data_rate,data_size,distance,duration,energy,frequency,humidity,illuminance,moisture,nitrogen,ph,phosphorus,pm1,pm10,pm25,pm4,potassium,pressure,signal_strength,temperature,universal,unspecified,uptime,voltage,water,weight,wind_speed")
                .Annotation("Npgsql:Enum:sensor_value_type", "bool,float,int,text,unspecified")
                .OldAnnotation("Npgsql:Enum:device_status", "active,blocked,pending")
                .OldAnnotation("Npgsql:Enum:func_arg_type", "bool,float,int,string,unspecified,void")
                .OldAnnotation("Npgsql:Enum:sensor_semantics", "aqi,area,battery,co2,current,data_rate,data_size,distance,duration,energy,frequency,humidity,illuminance,moisture,nitrogen,ph,phosphorus,pm1,pm10,pm25,pm4,potassium,pressure,signal_strength,temperature,universal,unspecified,uptime,voltage,water,weight,wind_speed")
                .OldAnnotation("Npgsql:Enum:sensor_value_type", "bool,float,int,text,unspecified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:device_status", "active,blocked,pending")
                .Annotation("Npgsql:Enum:func_arg_type", "bool,float,int,string,unspecified,void")
                .Annotation("Npgsql:Enum:sensor_semantics", "aqi,area,battery,co2,current,data_rate,data_size,distance,duration,energy,frequency,humidity,illuminance,moisture,nitrogen,ph,phosphorus,pm1,pm10,pm25,pm4,potassium,pressure,signal_strength,temperature,universal,unspecified,uptime,voltage,water,weight,wind_speed")
                .Annotation("Npgsql:Enum:sensor_value_type", "bool,float,int,text,unspecified")
                .OldAnnotation("Npgsql:Enum:device_status", "active,blocked,disconnected,pending")
                .OldAnnotation("Npgsql:Enum:func_arg_type", "bool,float,int,string,unspecified,void")
                .OldAnnotation("Npgsql:Enum:sensor_semantics", "aqi,area,battery,co2,current,data_rate,data_size,distance,duration,energy,frequency,humidity,illuminance,moisture,nitrogen,ph,phosphorus,pm1,pm10,pm25,pm4,potassium,pressure,signal_strength,temperature,universal,unspecified,uptime,voltage,water,weight,wind_speed")
                .OldAnnotation("Npgsql:Enum:sensor_value_type", "bool,float,int,text,unspecified");
        }
    }
}
