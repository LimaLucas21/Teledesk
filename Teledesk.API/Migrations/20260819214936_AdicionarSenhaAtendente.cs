using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teledesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSenhaAtendente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Senha",
                table: "Atendentes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Senha",
                table: "Atendentes");
        }
    }
}
