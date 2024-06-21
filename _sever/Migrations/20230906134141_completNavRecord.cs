using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations
{
    public partial class completNavRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PriorityLevel",
                table: "T_NavigationMenu",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ParentId",
                table: "T_NavigationMenu",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "T_NavigationMenu",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "T_NavigationMenu",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Path",
                table: "T_NavigationMenu");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "T_NavigationMenu");

            migrationBuilder.AlterColumn<int>(
                name: "PriorityLevel",
                table: "T_NavigationMenu",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentId",
                table: "T_NavigationMenu",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
