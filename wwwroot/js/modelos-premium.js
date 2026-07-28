/* =====================================================
   VISOR PREMIUM MODELOS DE UÑAS
   TropiNails Pro
===================================================== */


function abrirModeloPremium(id) {


    const contenedor = document.getElementById(
        "modelo-data-" + id
    );


    if(!contenedor)
        return;



    const modal = contenedor.querySelector(
        ".modelo-premium-overlay"
    );


    if(!modal)
        return;



    modal.style.display = "flex";


    document.body.style.overflow = "hidden";


}




function cerrarModeloPremium() {


    const modal = document.querySelector(
        ".modelo-premium-overlay"
    );


    if(!modal)
        return;



    modal.style.display = "none";


    document.body.style.overflow = "";



    const video = modal.querySelector("video");


    if(video){

        video.pause();

        video.currentTime = 0;

    }


}





// cerrar tocando fuera del contenido

document.addEventListener(
"click",
function(e){


    const modal = e.target.closest(
        ".modelo-premium-overlay"
    );


    if(!modal)
        return;



    if(e.target === modal){

        cerrarModeloPremium();

    }


});






// botón reservar diseño

document.addEventListener(
"click",
function(e){



    const boton = e.target.closest(
        ".modelo-premium-reservar"
    );



    if(!boton)
        return;



    const modeloId =
        boton.dataset.modeloId;



    console.log(
        "Modelo seleccionado:",
        modeloId
    );



    // Próximo paso:
    // enviar a Agendar con modeloId

});