using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIndicesMarcaModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_equipamento_Marca",
                table: "equipamento",
                column: "Marca");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_Modelo",
                table: "equipamento",
                column: "Modelo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_equipamento_Marca",
                table: "equipamento");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_Modelo",
                table: "equipamento");
        }
    }
}
