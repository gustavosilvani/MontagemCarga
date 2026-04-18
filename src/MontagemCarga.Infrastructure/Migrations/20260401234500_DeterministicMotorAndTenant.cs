using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontagemCarga.Infrastructure.Migrations
{
    public partial class DeterministicMotorAndTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bloco_carregamentos_carregamentos_CarregamentoId1",
                table: "bloco_carregamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_carregamento_pedidos_carregamentos_CarregamentoId1",
                table: "carregamento_pedidos");

            migrationBuilder.DropIndex(
                name: "IX_bloco_carregamentos_CarregamentoId1",
                table: "bloco_carregamentos");

            migrationBuilder.DropIndex(
                name: "IX_carregamento_pedidos_CarregamentoId1",
                table: "carregamento_pedidos");

            migrationBuilder.DropColumn(
                name: "CarregamentoId1",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "CarregamentoId1",
                table: "carregamento_pedidos");

            migrationBuilder.AddColumn<Guid>(
                name: "EmbarcadorId",
                table: "carregamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE carregamentos
                SET "EmbarcadorId" = '00000000-0000-0000-0000-000000000000'
                WHERE "EmbarcadorId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmbarcadorId",
                table: "carregamentos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "sequencia_carregamentos",
                columns: table => new
                {
                    FilialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UltimoNumero = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sequencia_carregamentos", x => x.FilialId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carregamentos_EmbarcadorId_CreatedAt",
                table: "carregamentos",
                columns: new[] { "EmbarcadorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_carregamentos_EmbarcadorId_FilialId_NumeroCarregamento",
                table: "carregamentos",
                columns: new[] { "EmbarcadorId", "FilialId", "NumeroCarregamento" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sequencia_carregamentos");

            migrationBuilder.DropIndex(
                name: "IX_carregamentos_EmbarcadorId_CreatedAt",
                table: "carregamentos");

            migrationBuilder.DropIndex(
                name: "IX_carregamentos_EmbarcadorId_FilialId_NumeroCarregamento",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "EmbarcadorId",
                table: "carregamentos");

            migrationBuilder.AddColumn<Guid>(
                name: "CarregamentoId1",
                table: "bloco_carregamentos",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "CarregamentoId1",
                table: "carregamento_pedidos",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_bloco_carregamentos_CarregamentoId1",
                table: "bloco_carregamentos",
                column: "CarregamentoId1");

            migrationBuilder.CreateIndex(
                name: "IX_carregamento_pedidos_CarregamentoId1",
                table: "carregamento_pedidos",
                column: "CarregamentoId1");

            migrationBuilder.AddForeignKey(
                name: "FK_bloco_carregamentos_carregamentos_CarregamentoId1",
                table: "bloco_carregamentos",
                column: "CarregamentoId1",
                principalTable: "carregamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_carregamento_pedidos_carregamentos_CarregamentoId1",
                table: "carregamento_pedidos",
                column: "CarregamentoId1",
                principalTable: "carregamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
