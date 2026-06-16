let destino = document.getElementById("chatDestino")?.value || "";
let usuarioActual = document.getElementById("chatUsuario")?.value || "";

const chatBody =
document.getElementById("chatBody");

const txtMensaje =
document.getElementById("txtMensaje");

const fileInput =
document.getElementById("fileInput");

const panel =
document.getElementById("emojiPanel");

/* ===================================================== */
/* SIGNALR */
/* ===================================================== */

const connection =
new signalR.HubConnectionBuilder()
.withUrl("/chatHub")
.withAutomaticReconnect()
.build();

async function iniciarConexion(){

    try{

        await connection.start();

        console.log("SignalR conectado");

    }catch{

        setTimeout(
            iniciarConexion,
            2000
        );
    }
}

iniciarConexion();

/* ===================================================== */
/* AGREGAR MENSAJES */
/* ===================================================== */

function agregarMensaje(
    texto,
    mio,
    tipo = "texto",
    mensajeId = 0
){

    let div =
    document.createElement("div");

    div.className =
    "msg " +
    (mio ? "mio" : "otro");

    div.dataset.id = mensajeId;

    /* MENSAJE ELIMINADO */

    if(tipo === "eliminado"){

        div.style.opacity = ".7";

        div.style.fontStyle = "italic";

        div.innerHTML =
        "🚫 Este mensaje fue eliminado";

        chatBody.appendChild(div);

        scrollChat();

        return;
    }

    /* IMAGEN */

    if(tipo === "imagen"){

        div.classList.add("msg-imagen");

        div.innerHTML = `
            <div class="img-wrapper">
                <img
                    src="${texto}"
                    class="chat-img"
                    onclick="abrirImagen('${texto}')">
            </div>
        `;
    }

    /* AUDIO */

    else if(tipo === "audio"){

        div.innerHTML =
        `<audio controls src="${texto}"></audio>`;
    }

    /* ARCHIVO */

    else if(tipo === "archivo"){

        div.innerHTML = `
            <a href="${texto}" target="_blank">
                📎 Descargar archivo
            </a>
        `;
    }

    /* TEXTO */

    else{

        div.innerText = texto;
    }

    /* MENU */

    let menu =
    document.createElement("span");

    menu.innerHTML = " ⋮ ";

    menu.style.cursor = "pointer";

    menu.style.float = "right";

    menu.style.fontWeight = "bold";

    menu.style.marginLeft = "10px";

    menu.onclick = async function(e){

        e.stopPropagation();

        let opcion =
        prompt(
            "1 Eliminar para mí\n2 Eliminar para todos"
        );

        if(opcion === "1"){

            div.remove();
        }

        if(opcion === "2"){

            await connection.invoke(
                "EliminarMensaje",
                mensajeId
            );
        }
    };

    div.appendChild(menu);

    chatBody.appendChild(div);

    scrollChat();
}

/* ===================================================== */
/* ENVIAR TEXTO */
/* ===================================================== */

async function enviarTexto(){

    let texto =
    txtMensaje.value.trim();

    if(!texto) return;

    await connection.invoke(
        "EnviarMensaje",
        destino,
        texto,
        "texto"
    );

    txtMensaje.value = "";
}

document.getElementById(
    "btnEnviar"
).onclick = enviarTexto;

txtMensaje.addEventListener(
    "keydown",
    function(e){

        if(
            e.key === "Enter"
            &&
            !e.shiftKey
        ){

            e.preventDefault();

            enviarTexto();
        }
    }
);

/* ===================================================== */
/* TYPING */
/* ===================================================== */

txtMensaje.addEventListener(
    "input",
    async()=>{

        try{

            await connection.invoke(
                "Typing",
                destino
            );

        }catch{}
    }
);

connection.on(
    "MostrarTyping",
    ()=>{

        let t =
        document.getElementById(
            "typingStatus"
        );

        t.style.display = "block";

        t.innerHTML =
        "✍️ escribiendo...";

        setTimeout(
            ()=> t.style.display = "none",
            1500
        );
    }
);

/* ===================================================== */
/* RECIBIR MENSAJES */
/* ===================================================== */

connection.on(
    "RecibirMensaje",
    (msg)=>{

        if(
            (
                msg.remitente === usuarioActual
                &&
                msg.destinatario === destino
            )
            ||
            (
                msg.remitente === destino
                &&
                msg.destinatario === usuarioActual
            )
        ){

            agregarMensaje(
                msg.contenido,
                msg.remitente === usuarioActual,
                msg.tipo,
                msg.id
            );
        }

        document.getElementById(
            "msgSound"
        )?.play();
    }
);

connection.on(
    "MensajeEliminado",
    ()=>{

        cargarHistorial();
    }
);

/* ===================================================== */
/* HISTORIAL */
/* ===================================================== */

async function cargarHistorial(){

    if(!destino) return;

    let res =
    await fetch(
        `/Chat/ObtenerMensajes?conUsuario=${destino}`
    );

    let result =
    await res.json();

    chatBody.innerHTML = "";

    (result.data || [])
    .forEach(m=>{

        agregarMensaje(
            m.contenido,
            m.remitente === usuarioActual,
            m.tipo,
            m.id
        );
    });
}

/* ===================================================== */
/* CONVERSACIONES */
/* ===================================================== */

async function cargarConversaciones(){

    let res =
    await fetch(
        "/Chat/ObtenerConversaciones"
    );

    let data =
    await res.json();

    let cont =
    document.getElementById(
        "listaConversaciones"
    );

    cont.innerHTML = "";

    data.forEach(c=>{

        let div =
        document.createElement("div");

        div.className =
        "conversacion";

        div.innerText =
        c.usuario;

        div.onclick = ()=>{

            window.location =
            "/Chat/Index?usuario=" +
            c.usuario;
        };

        cont.appendChild(div);
    });
}

/* ===================================================== */
/* BUSCAR */
/* ===================================================== */

document.getElementById(
    "buscarContacto"
)?.addEventListener(
    "keyup",
    function(){

        let filtro =
        this.value.toLowerCase();

        document.querySelectorAll(
            ".conversacion"
        )
        .forEach(c=>{

            c.style.display =
            c.innerText
            .toLowerCase()
            .includes(filtro)
            ? "block"
            : "none";
        });
    }
);

/* ===================================================== */
/* UTILIDADES */
/* ===================================================== */

function scrollChat(){

    chatBody.scrollTop =
    chatBody.scrollHeight;
}

function abrirImagen(url){

    let modal =
    document.createElement("div");

    modal.style.position = "fixed";

    modal.style.top = "0";

    modal.style.left = "0";

    modal.style.width = "100%";

    modal.style.height = "100%";

    modal.style.background =
    "rgba(0,0,0,0.85)";

    modal.style.display = "flex";

    modal.style.alignItems = "center";

    modal.style.justifyContent = "center";

    modal.style.zIndex = "99999";

    modal.innerHTML = `
        <img
            src="${url}"
            style="
                max-width:90%;
                max-height:90%;
                border-radius:15px;
            ">
    `;

    modal.onclick =
    ()=> modal.remove();

    document.body.appendChild(modal);
}

/* ===================================================== */
/* INICIAR */
/* ===================================================== */

cargarConversaciones();

cargarHistorial();