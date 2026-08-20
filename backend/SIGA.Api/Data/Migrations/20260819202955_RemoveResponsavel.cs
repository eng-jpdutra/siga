using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_equipamento_responsavel_ResponsavelId",
                table: "equipamento");

            migrationBuilder.DropTable(
                name: "responsavel");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_ResponsavelId",
                table: "equipamento");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "equipamento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsavelId",
                table: "equipamento",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "responsavel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Contato = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
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
                name: "IX_equipamento_ResponsavelId",
                table: "equipamento",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_responsavel_LocalId",
                table: "responsavel",
                column: "LocalId");

            migrationBuilder.AddForeignKey(
                name: "FK_equipamento_responsavel_ResponsavelId",
                table: "equipamento",
                column: "ResponsavelId",
                principalTable: "responsavel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
