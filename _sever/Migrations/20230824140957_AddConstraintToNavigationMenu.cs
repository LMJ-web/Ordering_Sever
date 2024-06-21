using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations
{
    public partial class AddConstraintToNavigationMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NavigationName",
                table: "T_NavigationMenu",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "T_NavigationMenu",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PriorityLevel",
                table: "T_NavigationMenu",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavigationName",
                table: "T_NavigationMenu");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "T_NavigationMenu");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "T_NavigationMenu");
        }
    }
}
