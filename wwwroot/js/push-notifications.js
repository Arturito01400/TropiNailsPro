// ============================================================
// TROPINAILS PRO
// PUSH NOTIFICATIONS
// Registro del Service Worker + suscripción Web Push
// ============================================================

(function () {

    "use strict";

    console.log(
        "[TropiNailsPro] Sistema Push cargado."
    );


    // ========================================================
    // CONFIGURACIÓN
    // ========================================================

    let VAPID_PUBLIC_KEY = "";


    // ========================================================
    // OBTENER VAPID PUBLIC KEY DESDE EL SERVIDOR
    // ========================================================
    //
    // La clave está almacenada en Azure App Service:
    //
    // VAPID:PublicKey
    //
    // Nunca colocamos la PrivateKey en JavaScript.
    //
    // ========================================================

    async function obtenerVapidPublicKey() {

        try {

            const response =
                await fetch(
                    "/Notificaciones/ObtenerVapidPublicKey",
                    {
                        method: "GET",
                        cache: "no-store",
                        credentials: "include"
                    }
                );


            if (!response.ok) {

                console.error(
                    "[TropiNailsPro] ❌ No se pudo obtener la VAPID Public Key.",
                    response.status
                );

                return null;
            }


            const resultado =
                await response.json();


            if (
                !resultado ||
                !resultado.success ||
                !resultado.publicKey
            ) {

                console.error(
                    "[TropiNailsPro] ❌ El servidor no devolvió una VAPID Public Key válida."
                );

                return null;
            }


            VAPID_PUBLIC_KEY =
                resultado.publicKey;


            console.log(
                "[TropiNailsPro] ✅ VAPID Public Key obtenida correctamente."
            );


            return VAPID_PUBLIC_KEY;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error obteniendo VAPID Public Key:",
                error
            );

            return null;
        }
    }


    // ========================================================
    // CONVERTIR BASE64URL → UINT8ARRAY
    // ========================================================

    function convertirClaveVapid(base64String) {

        const padding =
            "=".repeat(
                (4 - base64String.length % 4) % 4
            );


        const base64 =
            (base64String + padding)
                .replace(/-/g, "+")
                .replace(/_/g, "/");


        const rawData =
            window.atob(base64);


        return Uint8Array.from(
            [...rawData].map(
                char =>
                    char.charCodeAt(0)
            )
        );
    }


    // ========================================================
    // VERIFICAR SOPORTE
    // ========================================================

    function pushDisponible() {

        return (
            "serviceWorker" in navigator &&
            "PushManager" in window &&
            "Notification" in window
        );

    }


    // ========================================================
    // REGISTRAR SERVICE WORKER
    // ========================================================

    async function registrarServiceWorker() {

        try {

            const registro =
                await navigator.serviceWorker.register(
                    "/sw.js",
                    {
                        scope: "/"
                    }
                );


            console.log(
                "[TropiNailsPro] ✅ Service Worker registrado.",
                registro
            );


            return registro;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error registrando Service Worker:",
                error
            );


            return null;
        }
    }


    // ========================================================
    // SOLICITAR PERMISO
    // ========================================================

    async function solicitarPermiso() {

        try {

            const permiso =
                await Notification.requestPermission();


            console.log(
                "[TropiNailsPro] Permiso Push:",
                permiso
            );


            return permiso;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error solicitando permiso:",
                error
            );


            return "denied";
        }
    }


    // ========================================================
    // CREAR SUSCRIPCIÓN
    // ========================================================

    async function crearSuscripcion(registro) {

        try {

            // ------------------------------------------------
            // ASEGURAR VAPID PUBLIC KEY
            // ------------------------------------------------

            if (!VAPID_PUBLIC_KEY) {

                const clave =
                    await obtenerVapidPublicKey();


                if (!clave) {

                    console.error(
                        "[TropiNailsPro] ❌ No existe VAPID Public Key."
                    );

                    return null;
                }
            }


            // ------------------------------------------------
            // BUSCAR SUSCRIPCIÓN EXISTENTE
            // ------------------------------------------------

            let suscripcion =
                await registro.pushManager
                    .getSubscription();


            // ------------------------------------------------
            // SI YA EXISTE
            // ------------------------------------------------

            if (suscripcion) {

                console.log(
                    "[TropiNailsPro] ✅ Ya existe una suscripción Push."
                );


                return suscripcion;
            }


            // ------------------------------------------------
            // CREAR NUEVA SUSCRIPCIÓN
            // ------------------------------------------------

            suscripcion =
                await registro.pushManager.subscribe({

                    userVisibleOnly: true,

                    applicationServerKey:
                        convertirClaveVapid(
                            VAPID_PUBLIC_KEY
                        )

                });


            console.log(
                "[TropiNailsPro] ✅ Nueva suscripción Push creada."
            );


            return suscripcion;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error creando suscripción:",
                error
            );


            return null;
        }
    }


    // ========================================================
    // ENVIAR SUSCRIPCIÓN AL SERVIDOR
    // ========================================================

    async function enviarSuscripcionAlServidor(
        suscripcion
    ) {

        try {

            if (!suscripcion) {

                console.error(
                    "[TropiNailsPro] ❌ No existe suscripción Push."
                );

                return false;
            }


            const datos =
                suscripcion.toJSON();


            if (
                !datos.endpoint ||
                !datos.keys?.p256dh ||
                !datos.keys?.auth
            ) {

                console.error(
                    "[TropiNailsPro] ❌ La suscripción Push está incompleta."
                );

                return false;
            }


            const response =
                await fetch(
                    "/Notificaciones/RegistrarPush",
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        credentials: "include",

                        body:
                            JSON.stringify({

                                endpoint:
                                    datos.endpoint,

                                p256dh:
                                    datos.keys.p256dh,

                                auth:
                                    datos.keys.auth,

                                plataforma:
                                    navigator.platform,

                                navegador:
                                    navigator.userAgent,

                                userAgent:
                                    navigator.userAgent

                            })
                    }
                );


            if (!response.ok) {

                console.error(
                    "[TropiNailsPro] ❌ El servidor rechazó la suscripción.",
                    response.status
                );

                return false;
            }


            const resultado =
                await response.json();


            if (
                !resultado ||
                !resultado.success
            ) {

                console.error(
                    "[TropiNailsPro] ❌ El servidor no confirmó la suscripción.",
                    resultado
                );

                return false;
            }


            console.log(
                "[TropiNailsPro] ✅ Suscripción registrada en el servidor.",
                resultado
            );


            return true;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error enviando suscripción al servidor:",
                error
            );


            return false;
        }
    }


    // ========================================================
    // DESACTIVAR SUSCRIPCIÓN
    // ========================================================

    async function desactivarSuscripcion(
        suscripcion
    ) {

        try {

            if (!suscripcion) {

                return false;
            }


            const datos =
                suscripcion.toJSON();


            if (!datos.endpoint) {

                return false;
            }


            const response =
                await fetch(
                    "/Notificaciones/DesactivarPush",
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        credentials: "include",

                        body:
                            JSON.stringify({

                                endpoint:
                                    datos.endpoint

                            })
                    }
                );


            if (!response.ok) {

                console.warn(
                    "[TropiNailsPro] ⚠️ No se pudo desactivar la suscripción en el servidor."
                );

                return false;
            }


            console.log(
                "[TropiNailsPro] ✅ Suscripción desactivada en el servidor."
            );


            return true;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error desactivando suscripción:",
                error
            );


            return false;
        }
    }


    // ========================================================
    // INICIALIZAR PUSH
    // ========================================================

    async function inicializarPush() {

        try {

            // ------------------------------------------------
            // VERIFICAR SOPORTE
            // ------------------------------------------------

            if (!pushDisponible()) {

                console.warn(
                    "[TropiNailsPro] ⚠️ Este navegador no soporta Web Push."
                );

                return false;
            }


            // ------------------------------------------------
            // OBTENER VAPID PUBLIC KEY
            // ------------------------------------------------

            const clave =
                await obtenerVapidPublicKey();


            if (!clave) {

                return false;
            }


            // ------------------------------------------------
            // REGISTRAR SERVICE WORKER
            // ------------------------------------------------

            const registro =
                await registrarServiceWorker();


            if (!registro) {

                return false;
            }


            // ------------------------------------------------
            // ESPERAR SERVICE WORKER ACTIVO
            // ------------------------------------------------

            await navigator.serviceWorker.ready;


            // ------------------------------------------------
            // VERIFICAR PERMISO ACTUAL
            // ------------------------------------------------

            let permiso =
                Notification.permission;


            // ------------------------------------------------
            // SOLICITAR PERMISO SOLO SI AÚN NO EXISTE
            // ------------------------------------------------

            if (permiso === "default") {

                permiso =
                    await solicitarPermiso();
            }


            // ------------------------------------------------
            // PERMISO NO CONCEDIDO
            // ------------------------------------------------

            if (permiso !== "granted") {

                console.warn(
                    "[TropiNailsPro] ⚠️ Permiso Push no concedido."
                );

                return false;
            }


            // ------------------------------------------------
            // CREAR / RECUPERAR SUSCRIPCIÓN
            // ------------------------------------------------

            const suscripcion =
                await crearSuscripcion(
                    registro
                );


            if (!suscripcion) {

                return false;
            }


            // ------------------------------------------------
            // GUARDAR EN SERVIDOR
            // ------------------------------------------------

            return await enviarSuscripcionAlServidor(
                suscripcion
            );

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error inicializando Push:",
                error
            );


            return false;
        }
    }


    // ========================================================
    // OBTENER SUSCRIPCIÓN ACTUAL
    // ========================================================

    async function obtenerSuscripcion() {

        try {

            if (
                !("serviceWorker" in navigator)
            ) {

                return null;
            }


            const registro =
                await navigator.serviceWorker.ready;


            return await registro.pushManager
                .getSubscription();

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error obteniendo suscripción:",
                error
            );


            return null;
        }
    }


    // ========================================================
    // DESACTIVAR PUSH COMPLETAMENTE
    // ========================================================

    async function desactivarPush() {

        try {

            const suscripcion =
                await obtenerSuscripcion();


            if (!suscripcion) {

                return true;
            }


            await desactivarSuscripcion(
                suscripcion
            );


            const eliminada =
                await suscripcion.unsubscribe();


            if (eliminada) {

                console.log(
                    "[TropiNailsPro] ✅ Suscripción eliminada del navegador."
                );
            }


            return eliminada;

        }
        catch (error) {

            console.error(
                "[TropiNailsPro] ❌ Error desactivando Push:",
                error
            );


            return false;
        }
    }


    // ========================================================
    // EXPONER FUNCIONES GLOBALES
    // ========================================================

    window.TropiNailsPush = {

        iniciar:
            inicializarPush,

        registrarServiceWorker:
            registrarServiceWorker,

        solicitarPermiso:
            solicitarPermiso,

        obtenerVapidPublicKey:
            obtenerVapidPublicKey,

        obtenerSuscripcion:
            obtenerSuscripcion,

        desactivarPush:
            desactivarPush

    };


    // ========================================================
    // NO INICIAR AUTOMÁTICAMENTE
    // ========================================================
    //
    // IMPORTANTE:
    //
    // No hacemos:
    //
    // inicializarPush();
    //
    // automáticamente.
    //
    // La aplicación puede llamar:
    //
    // TropiNailsPush.iniciar();
    //
    // cuando corresponda.
    //
    // Esto evita solicitar permisos de notificación
    // inmediatamente al entrar a cualquier página.
    //
    // ========================================================


})();