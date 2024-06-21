using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations
{
    public partial class FixNavRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentName",
                table: "T_NavigationMenu",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentName",
                table: "T_NavigationMenu");
        }
    }
}
