using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nota_fiscal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Fornecedor = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    DataEmissao = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    ArquivoPath = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nota_fiscal", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_NotaFiscalId",
                table: "equipamento",
                column: "NotaFiscalId");

            migrationBuilder.AddForeignKey(
                name: "FK_equipamento_nota_fiscal_NotaFiscalId",
                table: "equipamento",
                column: "NotaFiscalId",
                principalTable: "nota_fiscal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_equipamento_nota_fiscal_NotaFiscalId",
                table: "equipamento");

            migrationBuilder.DropTable(
                name: "nota_fiscal");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_NotaFiscalId",
                table: "equipamento");
        }
    }
}
