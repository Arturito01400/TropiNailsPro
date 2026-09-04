// ============================================================
// TROPINAILS PRO
// SERVICE WORKER — NOTIFICACIONES PUSH
// ============================================================

console.log(
    "[TropiNailsPro] Service Worker cargado."
);


// ============================================================
// INSTALACIÓN
// ============================================================

self.addEventListener(
    "install",
    event => {

        console.log(
            "[TropiNailsPro] Service Worker instalado."
        );

        event.waitUntil(
            self.skipWaiting()
        );

    }
);


// ============================================================
// ACTIVACIÓN
// ============================================================

self.addEventListener(
    "activate",
    event => {

        console.log(
            "[TropiNailsPro] Service Worker activado."
        );

        event.waitUntil(
            self.clients.claim()
        );

    }
);


// ============================================================
// 🔥 PUSH RECIBIDO
// ============================================================

self.addEventListener(
    "push",
    event => {

        console.log(
            "[TropiNailsPro] 🔔 PUSH RECIBIDO."
        );


        // ====================================================
        // DATOS POR DEFECTO
        // ====================================================

        let datos = {

            title:
                "TropiNails Pro",

            body:
                "Tienes una nueva notificación.",

            icon:
                "/images/logo-tropinails.png",

            badge:
                "/images/logo-tropinails.png",

            url:
                "/"

        };


        // ====================================================
        // LEER PAYLOAD
        // ====================================================

        if (event.data) {

            try {

                const recibido =
                    event.data.json();

                datos = {

                    ...datos,
                    ...recibido

                };

            }
            catch (error) {

                console.warn(
                    "[TropiNailsPro] El payload no es JSON.",
                    error
                );


                try {

                    datos.body =
                        event.data.text();

                }
                catch (textError) {

                    console.error(
                        "[TropiNailsPro] Error leyendo Push.",
                        textError
                    );

                }

            }

        }


        // ====================================================
        // ASEGURAR URL VÁLIDA
        // ====================================================

        let urlDestino =
            datos.url || "/";


        if (
            typeof urlDestino !== "string" ||
            urlDestino.trim() === ""
        ) {

            urlDestino = "/";

        }


        // ====================================================
        // OPCIONES DE NOTIFICACIÓN
        // ====================================================

        const opciones = {

            body:
                datos.body ||
                "Tienes una nueva notificación.",

            icon:
                datos.icon ||
                "/images/logo-tropinails.png",

            badge:
                datos.badge ||
                "/images/logo-tropinails.png",

            vibrate: [
                200,
                100,
                200
            ],

            data: {

                url:
                    urlDestino

            },

            requireInteraction:
                false

        };


        // ====================================================
        // MOSTRAR NOTIFICACIÓN
        // ====================================================

        event.waitUntil(

            self.registration.showNotification(

                datos.title ||
                "TropiNails Pro",

                opciones

            )

        );

    }
);


// ============================================================
// 🔥 CLICK EN LA NOTIFICACIÓN
// ============================================================

self.addEventListener(
    "notificationclick",
    event => {

        console.log(
            "[TropiNailsPro] 🔔 Notificación seleccionada."
        );


        event.notification.close();


        const urlDestino =
            event.notification?.data?.url ||
            "/";


        event.waitUntil(

            self.clients
                .matchAll({

                    type: "window",

                    includeUncontrolled:
                        true

                })

                .then(
                    clientes => {

                        // =================================================
                        // BUSCAR VENTANA EXISTENTE
                        // =================================================

                        for (
                            const cliente of clientes
                        ) {

                            if (
                                "navigate" in cliente &&
                                "focus" in cliente
                            ) {

                                return cliente
                                    .navigate(urlDestino)
                                    .then(
                                        () =>
                                            cliente.focus()
                                    );

                            }

                        }


                        // =================================================
                        // SI NO EXISTE → ABRIR TROPINAILS
                        // =================================================

                        if (
                            self.clients.openWindow
                        ) {

                            return self.clients.openWindow(
                                urlDestino
                            );

                        }

                    }
                )

        );

    }
);


// ============================================================
// CIERRE DE NOTIFICACIÓN
// ============================================================

self.addEventListener(
    "notificationclose",
    event => {

        console.log(
            "[TropiNailsPro] Notificación cerrada."
        );

    }
);