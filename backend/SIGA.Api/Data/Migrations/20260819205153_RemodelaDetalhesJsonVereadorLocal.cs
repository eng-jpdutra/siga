using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemodelaDetalhesJsonVereadorLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "computador");

            migrationBuilder.DropTable(
                name: "dispositivo_rede");

            migrationBuilder.DropTable(
                name: "impressora");

            migrationBuilder.AddColumn<int>(
                name: "LocalId",
                table: "vereador",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "local",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detalhes",
                table: "equipamento",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoIp",
                table: "equipamento",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoMac",
                table: "equipamento",
                type: "TEXT",
                maxLength: 17,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "configuracao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Conteudo = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracao_equipamento_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "equipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vereador_LocalId",
                table: "vereador",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_EnderecoMac",
                table: "equipamento",
                column: "EnderecoMac",
                unique: true,
                filter: "EnderecoMac IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_configuracao_EquipamentoId",
                table: "configuracao",
                column: "EquipamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_vereador_local_LocalId",
                table: "vereador",
                column: "LocalId",
                principalTable: "local",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vereador_local_LocalId",
                table: "vereador");

            migrationBuilder.DropTable(
                name: "configuracao");

            migrationBuilder.DropIndex(
                name: "IX_vereador_LocalId",
                table: "vereador");

            migrationBuilder.DropIndex(
                name: "IX_equipamento_EnderecoMac",
                table: "equipamento");

            migrationBuilder.DropColumn(
                name: "LocalId",
                table: "vereador");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "local");

            migrationBuilder.DropColumn(
                name: "Detalhes",
                table: "equipamento");

            migrationBuilder.DropColumn(
                name: "EnderecoIp",
                table: "equipamento");

            migrationBuilder.DropColumn(
                name: "EnderecoMac",
                table: "equipamento");

            migrationBuilder.CreateTable(
                name: "computador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArmazenamentoGb = table.Column<int>(type: "INTEGER", nullable: true),
                    Processador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RamGb = table.Column<short>(type: "INTEGER", nullable: true),
                    SistemaOperacional = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Subtipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TipoArmazenamento = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_computador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_computador_equipamento_Id",
                        column: x => x.Id,
                        principalTable: "equipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispositivo_rede",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnderecoIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    EnderecoMac = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    NumPortas = table.Column<short>(type: "INTEGER", nullable: true),
                    Subtipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    VersaoFirmware = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispositivo_rede", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispositivo_rede_equipamento_Id",
                        column: x => x.Id,
                        principalTable: "equipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "impressora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Colorida = table.Column<bool>(type: "INTEGER", nullable: false),
                    Conexao = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ContadorPaginas = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoImpressao = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_impressora", x => x.Id);
                    table.ForeignKey(
                        name: "FK_impressora_equipamento_Id",
                        column: x => x.Id,
                        principalTable: "equipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dispositivo_rede_EnderecoMac",
                table: "dispositivo_rede",
                column: "EnderecoMac",
                unique: true,
                filter: "EnderecoMac IS NOT NULL");
        }
    }
}
