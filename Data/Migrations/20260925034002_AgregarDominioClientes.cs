using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDominioClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IngresosMensuales = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCredito", x => x.Id);
                    table.CheckConstraint("CK_SolicitudCredito_MontoPositivo", "[MontoSolicitado] > 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_ClienteId",
                table: "SolicitudesCredito",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_Estado",
                table: "SolicitudesCredito",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_FechaSolicitud",
                table: "SolicitudesCredito",
                column: "FechaSolicitud");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCredito");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
