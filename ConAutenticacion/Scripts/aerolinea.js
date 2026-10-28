// Comportamiento minimo de la aplicacion: contador de asientos (maximo 7)
// y campos que solo aceptan digitos.
(function () {
    var MAXIMO_ASIENTOS = 7;

    function marcados() {
        return document.querySelectorAll('.contador-asiento:checked').length;
    }

    function actualizarResumen() {
        var contador = document.getElementById('contadorAsientos');
        if (contador) contador.textContent = marcados();

        var boton = document.getElementById('btnContinuar');
        if (boton) boton.disabled = marcados() === 0;
    }

    document.addEventListener('change', function (evento) {
        var casilla = evento.target;
        if (!casilla.classList || !casilla.classList.contains('contador-asiento')) return;

        var mensaje = document.getElementById('mensajeAsientos');
        if (marcados() > MAXIMO_ASIENTOS) {
            casilla.checked = false;
            if (mensaje) mensaje.textContent = 'Puede seleccionar hasta ' + MAXIMO_ASIENTOS + ' asientos.';
        } else if (mensaje) {
            mensaje.textContent = '';
        }

        var etiqueta = casilla.closest('label');
        if (etiqueta) etiqueta.classList.toggle('seleccionado', casilla.checked);
        actualizarResumen();
    });

    document.querySelectorAll('input[data-solo-digitos]').forEach(function (campo) {
        campo.addEventListener('input', function () {
            this.value = this.value.replace(/\D/g, '');
        });
    });

    actualizarResumen();
})();
