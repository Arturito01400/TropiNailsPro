/* ===================================================== */
/* VARIABLES */
/* ===================================================== */

const localVideo =
document.getElementById("localVideo");

const remoteVideo =
document.getElementById("remoteVideo");

let peer = null;

let localStream = null;

let llamadaActiva = false;

let usuarioLlamadaActiva = null;

let esVideo = true;

let llamadaPendienteOffer = null;

let usuarioLlamando = null;

let tipoEntranteVideo = true;

let mediaRecorder;

let audioChunks = [];

let grabando = false;

/* ===================================================== */
/* RTC */
/* ===================================================== */

const rtcConfig = {

    iceServers:[
        {
            urls:"stun:stun.l.google.com:19302"
        }
    ]
};

/* ===================================================== */
/* CREAR PEER */
/* ===================================================== */

async function crearPeer(){

    peer =
    new RTCPeerConnection(
        rtcConfig
    );

    peer.onicecandidate = e => {

        if(e.candidate){

            connection.invoke(
                "EnviarIceCandidate",
                usuarioLlamadaActiva,
                JSON.stringify(
                    e.candidate
                )
            );
        }
    };

    peer.ontrack = e => {

        remoteVideo.srcObject =
        e.streams[0];
    };

    if(localStream){

        localStream
        .getTracks()
        .forEach(track=>{

            peer.addTrack(
                track,
                localStream
            );
        });
    }
}

/* ===================================================== */
/* LLAMADAS */
/* ===================================================== */

async function llamarAudio(){

    esVideo = false;

    await iniciarLlamada();
}

async function llamarVideo(){

    esVideo = true;

    await iniciarLlamada();
}

async function iniciarLlamada(){

    document.getElementById(
        "callUI"
    ).style.display = "flex";

    document.getElementById(
        "callUser"
    ).innerText =
    "Llamando a " + destino;

    usuarioLlamadaActiva =
    destino;

    llamadaActiva = true;

    document.getElementById(
        "ringTone"
    ).play();

    localStream =
    await navigator.mediaDevices
    .getUserMedia({
        audio:true,
        video:esVideo
    });

    localVideo.srcObject =
    localStream;

    await crearPeer();

    let offer =
    await peer.createOffer();

    await peer.setLocalDescription(
        offer
    );

    await connection.invoke(
        "EnviarOfertaLlamada",
        usuarioLlamadaActiva,
        JSON.stringify({
            offer:offer,
            video:esVideo
        })
    );
}

/* ===================================================== */
/* RECIBIR LLAMADA */
/* ===================================================== */

connection.on(
    "RecibirOfertaLlamada",
    (payload,de)=>{

        let incoming =
        JSON.parse(payload);

        llamadaPendienteOffer =
        incoming.offer;

        usuarioLlamando = de;

        tipoEntranteVideo =
        incoming.video;

        document.getElementById(
            "callerName"
        ).innerText =
        "📞 " + de + " te llama";

        document.getElementById(
            "incomingCallModal"
        ).style.display = "flex";

        document.getElementById(
            "ringTone"
        ).play();
    }
);

/* ===================================================== */
/* ACEPTAR */
/* ===================================================== */

document.getElementById(
    "btnAceptarLlamada"
).onclick =
async function(){

    document.getElementById(
        "incomingCallModal"
    ).style.display = "none";

    document.getElementById(
        "ringTone"
    ).pause();

    usuarioLlamadaActiva =
    usuarioLlamando;

    llamadaActiva = true;

    document.getElementById(
        "callUI"
    ).style.display = "flex";

    localStream =
    await navigator.mediaDevices
    .getUserMedia({
        audio:true,
        video:tipoEntranteVideo
    });

    localVideo.srcObject =
    localStream;

    await crearPeer();

    await peer.setRemoteDescription(
        new RTCSessionDescription(
            llamadaPendienteOffer
        )
    );

    let answer =
    await peer.createAnswer();

    await peer.setLocalDescription(
        answer
    );

    await connection.invoke(
        "EnviarRespuestaLlamada",
        usuarioLlamadaActiva,
        JSON.stringify(answer)
    );
};

/* ===================================================== */
/* RECHAZAR */
/* ===================================================== */

document.getElementById(
    "btnRechazarLlamada"
).onclick =
async function(){

    document.getElementById(
        "incomingCallModal"
    ).style.display = "none";

    document.getElementById(
        "ringTone"
    ).pause();

    await connection.invoke(
        "RechazarLlamada",
        usuarioLlamando
    );
};

/* ===================================================== */
/* RESPUESTA */
/* ===================================================== */

connection.on(
    "RecibirRespuestaLlamada",
    async(answerJson)=>{

        await peer.setRemoteDescription(
            new RTCSessionDescription(
                JSON.parse(answerJson)
            )
        );
    }
);

/* ===================================================== */
/* ICE */
/* ===================================================== */

connection.on(
    "RecibirIceCandidate",
    async candidate=>{

        if(peer){

            await peer.addIceCandidate(
                new RTCIceCandidate(
                    JSON.parse(candidate)
                )
            );
        }
    }
);

/* ===================================================== */
/* TERMINAR */
/* ===================================================== */

connection.on(
    "LlamadaTerminada",
    ()=>{

        colgarLlamada();
    }
);

function colgarLlamada(){

    if(!llamadaActiva)
    return;

    llamadaActiva = false;

    connection.invoke(
        "TerminarLlamada",
        usuarioLlamadaActiva
    );

    if(peer){

        peer.close();

        peer = null;
    }

    if(localStream){

        localStream
        .getTracks()
        .forEach(
            t=>t.stop()
        );
    }

    localVideo.srcObject = null;

    remoteVideo.srcObject = null;

    document.getElementById(
        "callUI"
    ).style.display = "none";

    usuarioLlamadaActiva = null;
}

/* ===================================================== */
/* NOTAS DE VOZ */
/* ===================================================== */

document.getElementById(
    "btnGrabar"
).onclick =
async function(){

    if(!grabando){

        const stream =
        await navigator.mediaDevices
        .getUserMedia({
            audio:true
        });

        audioChunks = [];

        mediaRecorder =
        new MediaRecorder(stream);

        mediaRecorder.start();

        grabando = true;

        this.innerHTML = "⏹";

        mediaRecorder.ondataavailable =
        e => audioChunks.push(
            e.data
        );

        mediaRecorder.onstop =
        async()=>{

            let blob =
            new Blob(
                audioChunks,
                {
                    type:"audio/webm"
                }
            );

            let formData =
            new FormData();

            formData.append(
                "audio",
                blob,
                "voz.webm"
            );

            formData.append(
                "destinatario",
                destino
            );

            let res =
            await fetch(
                "/Chat/SubirNotaVoz",
                {
                    method:"POST",
                    body:formData
                }
            );

            let data =
            await res.json();

            await connection.invoke(
                "EnviarMensaje",
                destino,
                data.url,
                "audio"
            );

            stream
            .getTracks()
            .forEach(
                t=>t.stop()
            );
        };

    }else{

        mediaRecorder.stop();

        grabando = false;

        this.innerHTML = "🎤";
    }
};

/* ===================================================== */
/* ARCHIVOS */
/* ===================================================== */

document.getElementById(
    "btnArchivo"
).onclick = ()=>{

    let menu =
    prompt(
        "1 Imagen\n2 Documento"
    );

    fileInput.accept =
    menu === "1"
    ? "image/*"
    : "*";

    fileInput.click();
};

fileInput.onchange =
async()=>{

    let file =
    fileInput.files[0];

    if(!file) return;

    let formData =
    new FormData();

    formData.append(
        "archivo",
        file
    );

    formData.append(
        "destinatario",
        destino
    );

    let res =
    await fetch(
        "/Chat/SubirArchivo",
        {
            method:"POST",
            body:formData
        }
    );

    await res.json();

    fileInput.value = "";
};