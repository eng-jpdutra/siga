using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaLicenca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "licenca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Produto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ChaveCriptografada = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true),
                    NotaFiscalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licenca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licenca_equipamento_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "equipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_licenca_nota_fiscal_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "nota_fiscal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_licenca_EquipamentoId",
                table: "licenca",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_licenca_NotaFiscalId",
                table: "licenca",
                column: "NotaFiscalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "licenca");
        }
    }
}
