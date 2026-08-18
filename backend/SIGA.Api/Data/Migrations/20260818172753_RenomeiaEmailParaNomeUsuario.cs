using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaEmailParaNomeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuario_Email",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "usuario");

            migrationBuilder.AddColumn<string>(
                name: "NomeUsuario",
                table: "usuario",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_NomeUsuario",
                table: "usuario",
                column: "NomeUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuario_NomeUsuario",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "NomeUsuario",
                table: "usuario");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "usuario",
                type: "TEXT",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_Email",
                table: "usuario",
                column: "Email",
                unique: true);
        }
    }
}
