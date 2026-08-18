using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGA.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialInventarioTi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Patrimonio = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NumeroSerie = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LocalId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResponsavelId = table.Column<int>(type: "INTEGER", nullable: true),
                    NotaFiscalId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AnoAquisicao = table.Column<short>(type: "INTEGER", nullable: true),
                    GarantiaAte = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "computador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subtipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SistemaOperacional = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RamGb = table.Column<short>(type: "INTEGER", nullable: true),
                    ArmazenamentoGb = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoArmazenamento = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Processador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
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
                    Subtipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EnderecoIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    EnderecoMac = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    NumPortas = table.Column<short>(type: "INTEGER", nullable: true),
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
                name: "historico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    RegistradoPor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    RegistradoEm = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historico_equipamento_EquipamentoId",
                        column: x => x.EquipamentoId,
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
                    TipoImpressao = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Colorida = table.Column<bool>(type: "INTEGER", nullable: false),
                    Conexao = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ContadorPaginas = table.Column<int>(type: "INTEGER", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_NumeroSerie",
                table: "equipamento",
                column: "NumeroSerie",
                unique: true,
                filter: "NumeroSerie IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_Patrimonio",
                table: "equipamento",
                column: "Patrimonio",
                unique: true,
                filter: "Patrimonio IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_Status",
                table: "equipamento",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_equipamento_Tipo",
                table: "equipamento",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_historico_EquipamentoId",
                table: "historico",
                column: "EquipamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "computador");

            migrationBuilder.DropTable(
                name: "dispositivo_rede");

            migrationBuilder.DropTable(
                name: "historico");

            migrationBuilder.DropTable(
                name: "impressora");

            migrationBuilder.DropTable(
                name: "equipamento");
        }
    }
}
