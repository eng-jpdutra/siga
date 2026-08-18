using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFotoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPath",
                table: "usuario",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPath",
                table: "usuario");
        }
    }
}
