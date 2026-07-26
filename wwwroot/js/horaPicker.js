/* ============================================================
   TropiNails Pro - Selector de Hora Profesional
   Archivo: horaPicker.js
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {

    inicializarHoraPicker();

});


function inicializarHoraPicker() {

    const contenedores = document.querySelectorAll(".hora-picker-container");


    contenedores.forEach(container => {

        const horaSelect = container.querySelector(".hora-picker-hora");
        const minutoSelect = container.querySelector(".hora-picker-minuto");
        const periodoSelect = container.querySelector(".hora-picker-periodo");

        const campoOculto = container.querySelector(".hora-picker-hidden");

        const fechaInput = document.querySelector("#Fecha");


        if (!horaSelect || !minutoSelect || !periodoSelect || !campoOculto) {
            return;
        }


        function actualizarHora() {

            let hora = parseInt(horaSelect.value);
            let minutos = minutoSelect.value;
            let periodo = periodoSelect.value;


            if (!hora || !periodo) {

                campoOculto.value = "";

                return;
            }


            // Conversión AM / PM a formato 24 horas

            if (periodo === "PM" && hora !== 12) {
                hora += 12;
            }


            if (periodo === "AM" && hora === 12) {
                hora = 0;
            }


            let horaFormato =
                hora.toString().padStart(2, "0")
                + ":"
                + minutos;


            campoOculto.value = horaFormato;


            validarHoraSeleccionada();

        }



        function validarHoraSeleccionada() {


            if (!fechaInput || !fechaInput.value) {
                return;
            }


            const fechaSeleccionada =
                new Date(fechaInput.value + "T00:00:00");


            const ahora = new Date();


            const hoy = new Date(
                ahora.getFullYear(),
                ahora.getMonth(),
                ahora.getDate()
            );


            const error =
                container.querySelector(".hora-picker-error");



            // Solo validar horas si la fecha es hoy

            if (fechaSeleccionada.getTime() === hoy.getTime()) {


                let hora24 =
                    convertirHora24(
                        horaSelect.value,
                        periodoSelect.value
                    );


                let minutos =
                    parseInt(minutoSelect.value);



                const fechaHoraSeleccionada =
                    new Date();


                fechaHoraSeleccionada.setHours(
                    hora24,
                    minutos,
                    0,
                    0
                );



                if (fechaHoraSeleccionada < ahora) {


                    if (error) {

                        error.textContent =
                            "No puedes seleccionar una hora que ya pasó.";

                        error.classList.add("activo");

                    }


                    campoOculto.value = "";


                    return false;

                }

            }


            if (error) {

                error.classList.remove("activo");

            }


            return true;

        }




        function bloquearHorasPasadas() {


            if (!fechaInput || !fechaInput.value) {
                return;
            }


            const fechaSeleccionada =
                new Date(fechaInput.value + "T00:00:00");


            const ahora = new Date();


            const hoy =
                new Date(
                    ahora.getFullYear(),
                    ahora.getMonth(),
                    ahora.getDate()
                );



            Array.from(horaSelect.options)
                .forEach(option => {


                    if (!option.value) {
                        return;
                    }


                    let hora24 =
                        convertirHora24(
                            option.value,
                            periodoSelect.value
                        );


                    let fechaPrueba =
                        new Date();


                    fechaPrueba.setHours(
                        hora24,
                        0,
                        0,
                        0
                    );



                    if (
                        fechaSeleccionada.getTime()
                        === hoy.getTime()
                        &&
                        fechaPrueba < ahora
                    ) {

                        option.disabled = true;

                    }
                    else {

                        option.disabled = false;

                    }


                });

        }




        function convertirHora24(hora, periodo) {

            hora = parseInt(hora);


            if (periodo === "PM" && hora !== 12) {
                hora += 12;
            }


            if (periodo === "AM" && hora === 12) {
                hora = 0;
            }


            return hora;

        }



        horaSelect.addEventListener(
            "change",
            actualizarHora
        );


        minutoSelect.addEventListener(
            "change",
            actualizarHora
        );


        periodoSelect.addEventListener(
            "change",
            function () {

                bloquearHorasPasadas();

                actualizarHora();

            }
        );



        if (fechaInput) {

            fechaInput.addEventListener(
                "change",
                function () {

                    bloquearHorasPasadas();

                    validarHoraSeleccionada();

                }
            );

        }



        bloquearHorasPasadas();


        // Si viene una hora existente desde Edit

        if (campoOculto.value) {


            cargarHoraExistente(
                campoOculto.value,
                horaSelect,
                minutoSelect,
                periodoSelect
            );


        }



    });


}





function cargarHoraExistente(
    valor,
    horaSelect,
    minutoSelect,
    periodoSelect
) {


    const partes = valor.split(":");


    if (partes.length !== 2) {
        return;
    }


    let hora =
        parseInt(partes[0]);


    let minutos =
        partes[1];



    let periodo =
        "AM";


    if (hora >= 12) {

        periodo = "PM";

        if (hora > 12) {
            hora -= 12;
        }

    }


    if (hora === 0) {

        hora = 12;

    }



    horaSelect.value =
        hora;


    minutoSelect.value =
        minutos;


    periodoSelect.value =
        periodo;

}