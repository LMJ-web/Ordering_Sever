using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations.OrderDb
{
    public partial class OrderInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.CreateTable(
                name: "T_CuisineType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityLevel = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_CuisineType", x => x.Id);
                });*/

            /*migrationBuilder.CreateTable(
                name: "T_NavigationMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NavigationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    ParentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityLevel = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_NavigationMenu", x => x.Id);
                });*/

            migrationBuilder.CreateTable(
                name: "T_Order",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TableNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Toatal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Order", x => x.Id);
                });

            /*migrationBuilder.CreateTable(
                name: "T_Cuisine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuisineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisinePictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisinePrice = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    CuisineDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    T_CuisineType_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Cuisine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_T_Cuisine_T_CuisineType_T_CuisineType_Id",
                        column: x => x.T_CuisineType_Id,
                        principalTable: "T_CuisineType",
                        principalColumn: "Id");
                });*/

            migrationBuilder.CreateTable(
                name: "T_OrderDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuisineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisinePrice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Amout = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_OrderDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_T_OrderDetail_T_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "T_Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_Cuisine_T_CuisineType_Id",
                table: "T_Cuisine",
                column: "T_CuisineType_Id");

            migrationBuilder.CreateIndex(
                name: "IX_T_OrderDetail_OrderId",
                table: "T_OrderDetail",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_Cuisine");

            migrationBuilder.DropTable(
                name: "T_NavigationMenu");

            migrationBuilder.DropTable(
                name: "T_OrderDetail");

            migrationBuilder.DropTable(
                name: "T_CuisineType");

            migrationBuilder.DropTable(
                name: "T_Order");
        }
    }
}
