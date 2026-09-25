"use strict";

(function () {
    if (typeof signalR === "undefined") {
        console.warn("SignalR no cargado, notificaciones deshabilitadas.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/solicitudes")
        .withAutomaticReconnect()
        .build();

    function mostrarNotificacion(data) {
        const contenedor = document.getElementById("notificaciones-solicitudes");
        if (!contenedor) return;

        const wrapper = document.createElement("div");
        const motivo = data.motivoRechazo ? `: ${data.motivoRechazo}` : "";
        wrapper.innerHTML = `
            <div class="alert alert-info alert-dismissible fade show" role="alert">
                Solicitud #${data.solicitudId} actualizada a <strong>${data.estado}</strong>${motivo}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        contenedor.prepend(wrapper.firstElementChild);

        // Actualizar badge en tabla si existe fila con ese id
        const fila = document.querySelector(`tr[data-solicitud-id="${data.solicitudId}"]`);
        if (fila) {
            const badge = fila.querySelector(".badge");
            if (badge) {
                badge.textContent = data.estado;
                badge.className = "badge " + (data.estado === "Aprobado" ? "bg-success" : data.estado === "Rechazado" ? "bg-danger" : "bg-warning text-dark");
            }
        }
    }

    connection.on("SolicitudEstadoActualizado", mostrarNotificacion);

    connection.onreconnected(() => {
        // Al reconectar, reconsultar estado actual vía endpoint existente (GET /Solicitudes)
        // para no depender de notificaciones perdidas durante la desconexión.
        window.location.reload();
    });

    connection.start().catch(function (err) {
        console.error("Error conectando a /hubs/solicitudes:", err.toString());
    });
})();
