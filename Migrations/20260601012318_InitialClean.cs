using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TropiNailsPro.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manicuristas_Usuarios_Id",
                table: "Manicuristas");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Manicuristas",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Manicuristas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Manicuristas_UsuarioId",
                table: "Manicuristas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manicuristas_Usuarios_UsuarioId",
                table: "Manicuristas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manicuristas_Usuarios_UsuarioId",
                table: "Manicuristas");

            migrationBuilder.DropIndex(
                name: "IX_Manicuristas_UsuarioId",
                table: "Manicuristas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Manicuristas");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Manicuristas",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddForeignKey(
                name: "FK_Manicuristas_Usuarios_Id",
                table: "Manicuristas",
                column: "Id",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
