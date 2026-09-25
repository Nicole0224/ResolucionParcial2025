using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteYSolicitudCredito : Migration
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
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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
                    PlazoMeses = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FechaEvaluacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCredito", x => x.Id);
                    table.CheckConstraint("CK_SolicitudCredito_MontoPositivo", "[MontoSolicitado] > 0");
                    table.CheckConstraint("CK_SolicitudCredito_PlazoValido", "[PlazoMeses] BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "FK_SolicitudesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Email",
                table: "Clientes",
                column: "Email",
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
