using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRestriccionesPregunta1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_ClienteId_PendienteUnico",
                table: "SolicitudesCredito",
                column: "ClienteId",
                unique: true,
                filter: "Estado = 'Pendiente'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Clientes_IngresosPositivo",
                table: "Clientes",
                sql: "[IngresosMensuales] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SolicitudesCredito_ClienteId_PendienteUnico",
                table: "SolicitudesCredito");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Clientes_IngresosPositivo",
                table: "Clientes");
        }
    }
}
