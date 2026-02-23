
function mostrarModalConRedireccion(mensaje, redireccionUrl) {
    console.log("Intentando mostrar modal de mensaje:", mensaje);

    // Cerrar cualquier modal abierto actualmente
      var modalesAbiertos = document.querySelectorAll('.modal.show');
    console.log("Modales actualmente abiertos:", modalesAbiertos.length);
    modalesAbiertos.forEach(function (modalElement) {
        console.log("Cerrando modal:", modalElement.id);
        var modalInstance = bootstrap.Modal.getInstance(modalElement);
        if (modalInstance) {
            modalInstance.hide();
            console.log("Modal ocultado:", modalElement.id);
        } else {
            console.log("No se encontró instancia de modal para:", modalElement.id);
        }
    });

    document.getElementById('modalMensajeContenido').innerText = mensaje;

    var modalEl = document.getElementById('modalMensaje');
    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    console.log("Mostrando modal de mensaje:", modalEl.id);
    modal.show();

    // Eliminar cualquier listener previo para evitar redirecciones múltiples o incorrectas
    // Es buena práctica si este modal puede ser usado para diferentes propósitos
    const oldRedirListener = modalEl.dataset.redirListener;
    if (oldRedirListener) {
        modalEl.removeEventListener('hidden.bs.modal', window[oldRedirListener]);
    }

    // Definir el listener de redirección aquí mismo o como una función anónima con la lógica condicional
    const newRedirListener = function redir() {
        console.log("Modal de mensaje se ha ocultado.");
        modalEl.removeEventListener('hidden.bs.modal', redir); // Elimina el listener después de ejecutarse

        // *** LA CLAVE ESTÁ AQUÍ: Redirigir SOLO si 'redireccionUrl' tiene un valor ***
        if (redireccionUrl && redireccionUrl.trim() !== '') { // Verifica que no sea nulo, vacío o solo espacios
            console.log("Redireccionando a:", redireccionUrl);
            window.location.href = redireccionUrl;
        } else {
            console.log("No se especificó URL de redirección. Manteniendo al usuario en la página.");
        }
    };

    modalEl.addEventListener('hidden.bs.modal', newRedirListener);
    modalEl.dataset.redirListener = newRedirListener.name || 'anonymousRedirListener'; // Guardar referencia para remover si es necesario
}

 


