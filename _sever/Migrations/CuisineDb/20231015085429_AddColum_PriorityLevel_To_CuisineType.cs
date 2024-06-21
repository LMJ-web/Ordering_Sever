using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations.CuisineDb
{
    public partial class AddColum_PriorityLevel_To_CuisineType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorityLevel",
                table: "T_CuisineType",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "T_CuisineType");
        }
    }
}
