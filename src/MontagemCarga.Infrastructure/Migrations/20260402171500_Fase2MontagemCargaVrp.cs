using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontagemCarga.Infrastructure.Migrations
{
    public partial class Fase2MontagemCargaVrp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChegadaEstimadaUtc",
                table: "bloco_carregamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanciaDesdeAnteriorKm",
                table: "bloco_carregamentos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DuracaoDesdeAnteriorMin",
                table: "bloco_carregamentos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "bloco_carregamentos",
                type: "double precision",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "bloco_carregamentos",
                type: "double precision",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaidaEstimadaUtc",
                table: "bloco_carregamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentroCarregamentoId",
                table: "carregamentos",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<decimal>(
                name: "CubagemCarregamento",
                table: "carregamentos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CustoSimulado",
                table: "carregamentos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanciaEstimadaKm",
                table: "carregamentos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DuracaoEstimadaMin",
                table: "carregamentos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "LatitudeCentro",
                table: "carregamentos",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudeCentro",
                table: "carregamentos",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroPaletesCarregamento",
                table: "carregamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OcupacaoCubagemPercentual",
                table: "carregamentos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OcupacaoPaletesPercentual",
                table: "carregamentos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OcupacaoPesoPercentual",
                table: "carregamentos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TipoMontagemCarregamentoVRP",
                table: "carregamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChegadaEstimadaUtc",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "DistanciaDesdeAnteriorKm",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "DuracaoDesdeAnteriorMin",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "SaidaEstimadaUtc",
                table: "bloco_carregamentos");

            migrationBuilder.DropColumn(
                name: "CentroCarregamentoId",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "CubagemCarregamento",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "CustoSimulado",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "DistanciaEstimadaKm",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "DuracaoEstimadaMin",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "LatitudeCentro",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "LongitudeCentro",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "NumeroPaletesCarregamento",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "OcupacaoCubagemPercentual",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "OcupacaoPaletesPercentual",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "OcupacaoPesoPercentual",
                table: "carregamentos");

            migrationBuilder.DropColumn(
                name: "TipoMontagemCarregamentoVRP",
                table: "carregamentos");
        }
    }
}
