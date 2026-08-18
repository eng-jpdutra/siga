using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaLocalEResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "local",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "responsavel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: true),
                    Contato = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsavel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_responsavel_local_LocalId",
                        column: x => x.LocalId,
                        principalTable: "local",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_LocalId",
                table: "equipamento",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_ResponsavelId",
                table: "equipamento",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_local_Nome",
                table: "local",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_responsavel_LocalId",
                table: "responsavel",
                column: "LocalId");

            migrationBuilder.AddForeignKey(
                name: "FK_equipamento_local_LocalId",
                table: "equipamento",
                column: "LocalId",
                principalTable: "local",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_equipamento_responsavel_ResponsavelId",
                table: "equipamento",
                column: "ResponsavelId",
                principalTable: "responsavel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_equipamento_local_LocalId",
                table: "equipamento");

            migrationBuilder.DropForeignKey(
                name: "FK_equipamento_responsavel_ResponsavelId",
                table: "equipamento");

            migrationBuilder.DropTable(
                name: "responsavel");

            migrationBuilder.DropTable(
                name: "local");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_LocalId",
                table: "equipamento");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_ResponsavelId",
                table: "equipamento");
        }
    }
}
