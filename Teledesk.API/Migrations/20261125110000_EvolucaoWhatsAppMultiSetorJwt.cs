using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teledesk.API.Migrations
{
    /// <inheritdoc />
    public partial class EvolucaoWhatsAppMultiSetorJwt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contatos_TelegramChatId",
                table: "Contatos");

            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                table: "Contatos");

            migrationBuilder.RenameColumn(
                name: "Senha",
                table: "Atendentes",
                newName: "SenhaHash");

            migrationBuilder.AlterColumn<string>(
                name: "SenhaHash",
                table: "Atendentes",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Atendentes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Perfil",
                table: "Atendentes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppId",
                table: "Contatos",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "NumeroWhatsAppId",
                table: "Atendimentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SetorId",
                table: "Atendimentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessageId",
                table: "Mensagens",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NumerosWhatsApp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NomeIdentificacao = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroExibicao = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumberId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumerosWhatsApp", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Setores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrdemUra = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Setores", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AtendenteSetores",
                columns: table => new
                {
                    AtendentesId = table.Column<int>(type: "int", nullable: false),
                    SetoresId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtendenteSetores", x => new { x.AtendentesId, x.SetoresId });
                    table.ForeignKey(
                        name: "FK_AtendenteSetores_Atendentes_AtendentesId",
                        column: x => x.AtendentesId,
                        principalTable: "Atendentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtendenteSetores_Setores_SetoresId",
                        column: x => x.SetoresId,
                        principalTable: "Setores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NumeroWhatsAppSetores",
                columns: table => new
                {
                    NumerosWhatsAppId = table.Column<int>(type: "int", nullable: false),
                    SetoresId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumeroWhatsAppSetores", x => new { x.NumerosWhatsAppId, x.SetoresId });
                    table.ForeignKey(
                        name: "FK_NumeroWhatsAppSetores_NumerosWhatsApp_NumerosWhatsAppId",
                        column: x => x.NumerosWhatsAppId,
                        principalTable: "NumerosWhatsApp",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NumeroWhatsAppSetores_Setores_SetoresId",
                        column: x => x.SetoresId,
                        principalTable: "Setores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Contatos_WhatsAppId",
                table: "Contatos",
                column: "WhatsAppId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atendimentos_NumeroWhatsAppId",
                table: "Atendimentos",
                column: "NumeroWhatsAppId");

            migrationBuilder.CreateIndex(
                name: "IX_Atendimentos_SetorId",
                table: "Atendimentos",
                column: "SetorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_WhatsAppMessageId",
                table: "Mensagens",
                column: "WhatsAppMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtendenteSetores_SetoresId",
                table: "AtendenteSetores",
                column: "SetoresId");

            migrationBuilder.CreateIndex(
                name: "IX_NumerosWhatsApp_PhoneNumberId",
                table: "NumerosWhatsApp",
                column: "PhoneNumberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumeroWhatsAppSetores_SetoresId",
                table: "NumeroWhatsAppSetores",
                column: "SetoresId");

            migrationBuilder.CreateIndex(
                name: "IX_Setores_Nome",
                table: "Setores",
                column: "Nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Atendimentos_NumerosWhatsApp_NumeroWhatsAppId",
                table: "Atendimentos",
                column: "NumeroWhatsAppId",
                principalTable: "NumerosWhatsApp",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Atendimentos_Setores_SetorId",
                table: "Atendimentos",
                column: "SetorId",
                principalTable: "Setores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Atendimentos_NumerosWhatsApp_NumeroWhatsAppId",
                table: "Atendimentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Atendimentos_Setores_SetorId",
                table: "Atendimentos");

            migrationBuilder.DropTable(
                name: "AtendenteSetores");

            migrationBuilder.DropTable(
                name: "NumeroWhatsAppSetores");

            migrationBuilder.DropTable(
                name: "NumerosWhatsApp");

            migrationBuilder.DropTable(
                name: "Setores");

            migrationBuilder.DropIndex(
                name: "IX_Contatos_WhatsAppId",
                table: "Contatos");

            migrationBuilder.DropIndex(
                name: "IX_Atendimentos_NumeroWhatsAppId",
                table: "Atendimentos");

            migrationBuilder.DropIndex(
                name: "IX_Atendimentos_SetorId",
                table: "Atendimentos");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_WhatsAppMessageId",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Atendentes");

            migrationBuilder.DropColumn(
                name: "Perfil",
                table: "Atendentes");

            migrationBuilder.DropColumn(
                name: "WhatsAppId",
                table: "Contatos");

            migrationBuilder.DropColumn(
                name: "NumeroWhatsAppId",
                table: "Atendimentos");

            migrationBuilder.DropColumn(
                name: "SetorId",
                table: "Atendimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessageId",
                table: "Mensagens");

            migrationBuilder.RenameColumn(
                name: "SenhaHash",
                table: "Atendentes",
                newName: "Senha");

            migrationBuilder.AlterColumn<string>(
                name: "Senha",
                table: "Atendentes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "TelegramChatId",
                table: "Contatos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Contatos_TelegramChatId",
                table: "Contatos",
                column: "TelegramChatId",
                unique: true);
        }
    }
}

