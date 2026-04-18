using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontagemCarga.Infrastructure.Migrations
{
    public partial class AdicionarRouteGeometryCarregamentos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "route_geometry",
                table: "carregamentos",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "route_geometry",
                table: "carregamentos");
        }
    }
}
