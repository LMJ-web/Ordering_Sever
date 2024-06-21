using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations.OrderDb
{
    public partial class addPayState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           /* migrationBuilder.DropColumn(
                name: "Toatal",
                table: "T_Order");*/

            migrationBuilder.AlterColumn<decimal>(
                name: "CuisinePrice",
                table: "T_OrderDetail",
                type: "decimal(7,2)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amout",
                table: "T_OrderDetail",
                type: "decimal(7,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "PayState",
                table: "T_Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            /*migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "T_Order",
                type: "decimal(7,2)",
                nullable: false,
                defaultValue: 0m);*/
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayState",
                table: "T_Order");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "T_Order");

            migrationBuilder.AlterColumn<string>(
                name: "CuisinePrice",
                table: "T_OrderDetail",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amout",
                table: "T_OrderDetail",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "Toatal",
                table: "T_Order",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
