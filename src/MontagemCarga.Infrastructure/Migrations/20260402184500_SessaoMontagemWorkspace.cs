using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontagemCarga.Infrastructure.Migrations
{
    public partial class SessaoMontagemWorkspace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sessao_montagem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmbarcadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperadorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperadorNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FilialId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    ParametrosJson = table.Column<string>(type: "jsonb", nullable: false),
                    PedidosJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultadoJson = table.Column<string>(type: "jsonb", nullable: false),
                    NumerosCarregamentoReservadosJson = table.Column<string>(type: "jsonb", nullable: false),
                    CarregamentosCriadosJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PersistidaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessao_montagem", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sessao_montagem_EmbarcadorId_CreatedAt",
                table: "sessao_montagem",
                columns: new[] { "EmbarcadorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sessao_montagem_EmbarcadorId_FilialId_Situacao",
                table: "sessao_montagem",
                columns: new[] { "EmbarcadorId", "FilialId", "Situacao" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sessao_montagem");
        }
    }
}
