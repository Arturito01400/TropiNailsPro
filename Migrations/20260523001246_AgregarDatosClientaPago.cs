using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TropiNailsPro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosClientaPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPerfil_Usuarios_UsuarioId",
                table: "UsuariosPerfil");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosPerfil_Usuario",
                table: "UsuariosPerfil");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosPerfil_UsuarioId",
                table: "UsuariosPerfil");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "UsuariosPerfil");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "UsuariosPerfil");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "UsuariosPerfil");

            migrationBuilder.DropColumn(
                name: "Usuario",
                table: "UsuariosPerfil");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "UsuariosPerfil",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Instagram",
                table: "UsuariosPerfil",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "TipoMedia",
                table: "Publicaciones",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Texto",
                table: "Publicaciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClienteFoto",
                table: "PagosTransferencia",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClienteNombre",
                table: "PagosTransferencia",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ManicuristaId",
                table: "PagosTransferencia",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPerfil_UsuarioId",
                table: "UsuariosPerfil",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPerfil_Usuarios_UsuarioId",
                table: "UsuariosPerfil",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosPerfil_Usuarios_UsuarioId",
                table: "UsuariosPerfil");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosPerfil_UsuarioId",
                table: "UsuariosPerfil");

            migrationBuilder.DropColumn(
                name: "ClienteFoto",
                table: "PagosTransferencia");

            migrationBuilder.DropColumn(
                name: "ClienteNombre",
                table: "PagosTransferencia");

            migrationBuilder.DropColumn(
                name: "ManicuristaId",
                table: "PagosTransferencia");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "UsuariosPerfil",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "UsuariosPerfil",
                keyColumn: "Instagram",
                keyValue: null,
                column: "Instagram",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Instagram",
                table: "UsuariosPerfil",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "UsuariosPerfil",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "UsuariosPerfil",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "UsuariosPerfil",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Usuario",
                table: "UsuariosPerfil",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Publicaciones",
                keyColumn: "TipoMedia",
                keyValue: null,
                column: "TipoMedia",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "TipoMedia",
                table: "Publicaciones",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Publicaciones",
                keyColumn: "Texto",
                keyValue: null,
                column: "Texto",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Texto",
                table: "Publicaciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPerfil_Usuario",
                table: "UsuariosPerfil",
                column: "Usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPerfil_UsuarioId",
                table: "UsuariosPerfil",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosPerfil_Usuarios_UsuarioId",
                table: "UsuariosPerfil",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }
    }
}
