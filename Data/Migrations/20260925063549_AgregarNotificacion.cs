using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNotificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FechaProcesamientoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_SolicitudesCredito_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesCredito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_MessageId",
                table: "Notificaciones",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_SolicitudId",
                table: "Notificaciones",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificaciones");
        }
    }
}
