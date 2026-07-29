/* =====================================================
   VISOR PREMIUM MODELOS DE UÑAS
   TropiNails Pro
===================================================== */

function abrirModeloPremium(id) {

    const tarjeta = document.querySelector(
        `.modelo-card[onclick="abrirModeloPremium(${id})"]`
    );

    if (!tarjeta) {
        console.error("No existe la tarjeta del modelo:", id);
        return;
    }

    const modal = document.getElementById("modeloPremiumModal");
    const media = document.getElementById("modeloPremiumMedia");
    const nombre = document.getElementById("modeloPremiumNombre");
    const descripcion = document.getElementById("modeloPremiumDescripcion");
    const botonReservar = document.getElementById("btnReservarModelo");

    if (!modal || !media || !nombre || !descripcion || !botonReservar) {
        console.error("El modal premium no está correctamente construido.");
        return;
    }

    const contenido = tarjeta.querySelector(".modelo-media");

    media.innerHTML = "";

    if (contenido) {

        if (contenido.tagName === "IMG") {

            media.innerHTML = `
                <img
                    src="${contenido.src}"
                    class="modelo-premium-content"
                    alt="">
            `;

        }
        else if (contenido.tagName === "VIDEO") {

            media.innerHTML = `
                <video
                    class="modelo-premium-content"
                    controls
                    autoplay>

                    <source src="${contenido.currentSrc}" type="video/mp4">

                </video>
            `;
        }

    }

    nombre.textContent =
        tarjeta.querySelector("strong")?.textContent ?? "";

    descripcion.textContent =
        tarjeta.querySelector("small")?.textContent ?? "";

    botonReservar.dataset.modeloId = id;

    modal.style.display = "flex";

    document.body.style.overflow = "hidden";
}



function cerrarModeloPremium() {

    const modal = document.getElementById("modeloPremiumModal");

    if (!modal)
        return;

    const media = document.getElementById("modeloPremiumMedia");

    const video = media.querySelector("video");

    if (video) {

        video.pause();
        video.currentTime = 0;

    }

    media.innerHTML = "";

    modal.style.display = "none";

    document.body.style.overflow = "";

}



// Cerrar tocando el fondo oscuro

document.addEventListener("click", function (e) {

    if (e.target.id === "modeloPremiumModal") {

        cerrarModeloPremium();

    }

});



// Botón reservar

document.addEventListener("click", function (e) {

    const boton = e.target.closest("#btnReservarModelo");

    if (!boton)
        return;

    const modeloId = boton.dataset.modeloId;

    console.log("Modelo seleccionado:", modeloId);

    // Próximo paso:
    // window.location.href =
    // `/Agendar/Index?modeloId=${modeloId}`;

});