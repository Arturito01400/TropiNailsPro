// ============================================================
// TROPINAILS PRO
// SERVICE WORKER — NOTIFICACIONES PUSH
// ============================================================

const CACHE_NAME = "tropinails-pro-v1";

// ============================================================
// INSTALACIÓN
// ============================================================

self.addEventListener("install", event => {
    console.log("[TropiNailsPro] Service Worker instalado.");

    self.skipWaiting();
});


// ============================================================
// ACTIVACIÓN
// ============================================================

self.addEventListener("activate", event => {
    console.log("[TropiNailsPro] Service Worker activado.");

    event.waitUntil(
        self.clients.claim()
    );
});


// ============================================================
// NOTIFICACIÓN PUSH RECIBIDA
// ============================================================

self.addEventListener("push", event => {

    console.log("[TropiNailsPro] Push recibido.");

    let datos = {
        title: "TropiNailsPro",
        body: "Tienes una nueva notificación.",
        icon: "/images/logo-tropinails.png",
        badge: "/images/logo-tropinails.png",
        url: "/"
    };

    // --------------------------------------------------------
    // Intentar leer el JSON enviado por el servidor
    // --------------------------------------------------------

    if (event.data) {

        try {

            const recibido = event.data.json();

            datos = {
                ...datos,
                ...recibido
            };

        } catch (error) {

            console.warn(
                "[TropiNailsPro] No se pudo interpretar el payload:",
                error
            );

            try {

                datos.body = event.data.text();

            } catch (textError) {

                console.error(
                    "[TropiNailsPro] Error leyendo mensaje:",
                    textError
                );
            }
        }
    }

    // --------------------------------------------------------
    // Mostrar notificación
    // --------------------------------------------------------

    const opciones = {

        body: datos.body,

        icon: datos.icon || "/images/logo-tropinails.png",

        badge: datos.badge || "/images/logo-tropinails.png",

        vibrate: [200, 100, 200],

        data: {
            url: datos.url || "/"
        },

        requireInteraction: false,

        actions: [
            {
                action: "abrir",
                title: "Abrir TropiNails"
            }
        ]
    };

    event.waitUntil(

        self.registration.showNotification(
            datos.title || "TropiNailsPro",
            opciones
        )
    );
});


// ============================================================
// CLICK EN LA NOTIFICACIÓN
// ============================================================

self.addEventListener("notificationclick", event => {

    console.log(
        "[TropiNailsPro] Notificación seleccionada."
    );

    event.notification.close();

    const urlDestino =
        event.notification?.data?.url || "/";

    event.waitUntil(

        clients.matchAll({
            type: "window",
            includeUncontrolled: true
        })
        .then(clientes => {

            // ------------------------------------------------
            // Si TropiNails ya está abierto,
            // llevarlo al lugar correspondiente.
            // ------------------------------------------------

            for (const cliente of clientes) {

                if ("focus" in cliente) {

                    return cliente
                        .navigate(urlDestino)
                        .then(() => cliente.focus());
                }
            }

            // ------------------------------------------------
            // Si no está abierto, abrirlo.
            // ------------------------------------------------

            if (clients.openWindow) {

                return clients.openWindow(urlDestino);
            }

        })
    );
});


// ============================================================
// CIERRE DE NOTIFICACIÓN
// ============================================================

self.addEventListener("notificationclose", event => {

    console.log(
        "[TropiNailsPro] Notificación cerrada."
    );
});


// ============================================================
// FETCH
// ============================================================
// No interceptamos las peticiones normales de la aplicación.
// Esto es IMPORTANTE para no romper:
// - Login
// - SignalR
// - Chat
// - Imágenes
// - Azure Blob
// - Google Maps
// - Formularios
// - API
// ============================================================

self.addEventListener("fetch", event => {

    // Intencionalmente vacío.
    //
    // TropiNailsPro utiliza el Service Worker
    // principalmente para Web Push.
    //
    // No modificamos las peticiones normales.
});