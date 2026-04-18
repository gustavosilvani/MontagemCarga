using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontagemCarga.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carregamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroCarregamento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SituacaoCarregamento = table.Column<int>(type: "integer", nullable: false),
                    TipoMontagemCarga = table.Column<int>(type: "integer", nullable: false),
                    ModeloVeicularId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataCarregamentoCarga = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PesoCarregamento = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TipoDeCargaId = table.Column<Guid>(type: "uuid", nullable: true),
                    TipoOperacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilialId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carregamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bloco_carregamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarregamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoIdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Bloco = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrdemCarregamento = table.Column<int>(type: "integer", nullable: false),
                    OrdemEntrega = table.Column<int>(type: "integer", nullable: false),
                    CarregamentoId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bloco_carregamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bloco_carregamentos_carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bloco_carregamentos_carregamentos_CarregamentoId1",
                        column: x => x.CarregamentoId1,
                        principalTable: "carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carregamento_pedidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarregamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoIdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Peso = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Pallet = table.Column<int>(type: "integer", nullable: true),
                    VolumeTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    CarregamentoId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carregamento_pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carregamento_pedidos_carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_carregamento_pedidos_carregamentos_CarregamentoId1",
                        column: x => x.CarregamentoId1,
                        principalTable: "carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bloco_carregamentos_CarregamentoId",
                table: "bloco_carregamentos",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_bloco_carregamentos_CarregamentoId1",
                table: "bloco_carregamentos",
                column: "CarregamentoId1");

            migrationBuilder.CreateIndex(
                name: "IX_carregamento_pedidos_CarregamentoId",
                table: "carregamento_pedidos",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_carregamento_pedidos_CarregamentoId1",
                table: "carregamento_pedidos",
                column: "CarregamentoId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bloco_carregamentos");

            migrationBuilder.DropTable(
                name: "carregamento_pedidos");

            migrationBuilder.DropTable(
                name: "carregamentos");
        }
    }
}
