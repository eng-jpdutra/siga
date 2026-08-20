using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdentidadePropria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_papel");

            migrationBuilder.DropTable(
                name: "papel");

            migrationBuilder.DropTable(
                name: "usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "papel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_papel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResponsavelId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FotoPath = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NomeUsuario = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    TrocaSenhaObrigatoria = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuario_responsavel_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "responsavel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "usuario_papel",
                columns: table => new
                {
                    PapeisId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuariosId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_papel", x => new { x.PapeisId, x.UsuariosId });
                    table.ForeignKey(
                        name: "FK_usuario_papel_papel_PapeisId",
                        column: x => x.PapeisId,
                        principalTable: "papel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_papel_usuario_UsuariosId",
                        column: x => x.UsuariosId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_papel_Nome",
                table: "papel",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_NomeUsuario",
                table: "usuario",
                column: "NomeUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_ResponsavelId",
                table: "usuario",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_papel_UsuariosId",
                table: "usuario_papel",
                column: "UsuariosId");
        }
    }
}
