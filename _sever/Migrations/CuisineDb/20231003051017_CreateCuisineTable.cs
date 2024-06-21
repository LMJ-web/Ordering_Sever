using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _sever.Migrations.CuisineDb
{
    public partial class CreateCuisineTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_Cuisine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuisineName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisinePictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisineType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CuisinePrice = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    CuisineDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Cuisine", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_Cuisine");

            migrationBuilder.DropTable(
                name: "T_NavigationMenu");
        }
    }
}
