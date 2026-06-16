/* ===================================================== */
/* 🌸 TROPINAILS PRO */
/* REAL AUDIO + VIDEO CALLS */
/* ===================================================== */

let localStream = null;

let remoteStream = null;

let peerConnection = null;

let currentCallType = null;

let llamadaPendiente = null;

let ringtone = null;

/* ===================================================== */
/* 🔥 AUDIO CONTEXT FIX */
/* ===================================================== */

let audioContext = null;

async function activarAudioSistema(){

    try{

        if(!audioContext){

            audioContext =
            new (
                window.AudioContext ||
                window.webkitAudioContext
            )();
        }

        if(audioContext.state === "suspended"){

            await audioContext.resume();
        }

    }catch(error){

        console.log(
            "AudioContext error:",
            error
        );
    }
}

/* ===================================================== */
/* SIGNALR GLOBAL */
/* ===================================================== */

const callConnection =
new signalR.HubConnectionBuilder()
.withUrl("/chatHub")
.withAutomaticReconnect()
.build();

async function iniciarCallConnection(){

    try{

        await callConnection.start();

        console.log(
            "📞 SignalR llamadas conectado"
        );

    }catch(error){

        console.error(error);

        setTimeout(
            iniciarCallConnection,
            2000
        );
    }
}

iniciarCallConnection();

/* ===================================================== */
/* RTC CONFIG */
/* ===================================================== */

const rtcConfig = {

    iceServers: [

        {
            urls:"stun:stun.l.google.com:19302"
        }

    ]
};

/* ===================================================== */
/* SONIDO LLAMADA */
/* ===================================================== */

function crearElementoAudio(){

    let audio =
    document.getElementById(
        "ringtoneAudio"
    );

    if(!audio){

        audio =
        document.createElement("audio");

        audio.id = "ringtoneAudio";

        audio.loop = true;

        audio.preload = "auto";

        audio.playsInline = true;

        audio.src =
        "https://actions.google.com/sounds/v1/alarms/alarm_clock.ogg";

        document.body.appendChild(audio);
    }

    return audio;
}

async function reproducirTono(){

    try{

        await activarAudioSistema();

        detenerTono();

        ringtone =
        crearElementoAudio();

        ringtone.volume = 1;

        ringtone.currentTime = 0;

        const playPromise =
        ringtone.play();

        if(playPromise !== undefined){

            playPromise
            .then(()=>{

                console.log(
                    "🔔 Tono reproduciendo"
                );

            })
            .catch(error=>{

                console.log(
                    "Error tono:",
                    error
                );

                setTimeout(async()=>{

                    try{

                        await ringtone.play();

                    }catch{}
                },500);
            });
        }

    }catch(error){

        console.log(
            "No se pudo reproducir tono:",
            error
        );
    }
}

function detenerTono(){

    if(ringtone){

        ringtone.pause();

        ringtone.currentTime = 0;
    }
}

/* ===================================================== */
/* ACTIVAR AUDIO EN PRIMER CLICK */
/* ===================================================== */

document.addEventListener(
    "click",
    activarAudioSistema,
    {
        once:true
    }
);

document.addEventListener(
    "touchstart",
    activarAudioSistema,
    {
        once:true
    }
);

/* ===================================================== */
/* ELEMENTOS VIDEO */
/* ===================================================== */

const localVideo =
document.createElement("video");

const remoteVideo =
document.createElement("video");

localVideo.autoplay = true;
localVideo.muted = true;
localVideo.playsInline = true;

remoteVideo.autoplay = true;
remoteVideo.playsInline = true;

/* ===================================================== */
/* CREAR UI */
/* ===================================================== */

function crearUIllamada(){

    if(
        document.getElementById(
            "callOverlay"
        )
    ) return;

    let overlay =
    document.createElement("div");

    overlay.id = "callOverlay";

    overlay.style.position = "fixed";
    overlay.style.top = "0";
    overlay.style.left = "0";
    overlay.style.width = "100%";
    overlay.style.height = "100%";
    overlay.style.background = "#000";
    overlay.style.zIndex = "999999";

    overlay.style.display = "flex";
    overlay.style.flexDirection = "column";
    overlay.style.alignItems = "center";
    overlay.style.justifyContent = "center";

    remoteVideo.style.width = "100%";
    remoteVideo.style.height = "100%";
    remoteVideo.style.objectFit = "cover";

    localVideo.style.position = "absolute";
    localVideo.style.bottom = "20px";
    localVideo.style.right = "20px";

    localVideo.style.width = "140px";
    localVideo.style.height = "200px";

    localVideo.style.objectFit = "cover";

    localVideo.style.borderRadius = "15px";

    localVideo.style.border =
    "2px solid #fff";

    let endBtn =
    document.createElement("button");

    endBtn.innerHTML = "📞";

    endBtn.style.position = "absolute";

    endBtn.style.bottom = "40px";

    endBtn.style.left = "50%";

    endBtn.style.transform =
    "translateX(-50%)";

    endBtn.style.width = "70px";

    endBtn.style.height = "70px";

    endBtn.style.borderRadius = "50%";

    endBtn.style.border = "none";

    endBtn.style.background =
    "#ff2d55";

    endBtn.style.color = "#fff";

    endBtn.style.fontSize = "30px";

    endBtn.style.cursor = "pointer";

    endBtn.onclick = finalizarLlamada;

    overlay.appendChild(remoteVideo);

    overlay.appendChild(localVideo);

    overlay.appendChild(endBtn);

    document.body.appendChild(overlay);
}

/* ===================================================== */
/* UI LLAMADA ENTRANTE */
/* ===================================================== */

function mostrarLlamadaEntrante(
    remitente,
    tipo
){

    reproducirTono();

    if(
        document.getElementById(
            "incomingCall"
        )
    ) return;

    let div =
    document.createElement("div");

    div.id = "incomingCall";

    div.style.position = "fixed";
    div.style.top = "50%";
    div.style.left = "50%";
    div.style.transform =
    "translate(-50%,-50%)";

    div.style.width = "320px";

    div.style.background = "#fff";

    div.style.borderRadius = "25px";

    div.style.padding = "25px";

    div.style.boxShadow =
    "0 15px 40px rgba(0,0,0,.25)";

    div.style.zIndex = "999999";

    div.style.textAlign = "center";

    div.innerHTML = `
        <div style="
            font-size:70px;
            margin-bottom:10px;
        ">
            ${tipo === "video" ? "🎥" : "📞"}
        </div>

        <h2 style="
            margin-bottom:10px;
            color:#222;
        ">
            Llamada entrante
        </h2>

        <p style="
            color:#666;
            margin-bottom:25px;
            font-size:16px;
        ">
            ${remitente}
        </p>

        <div style="
            display:flex;
            justify-content:center;
            gap:20px;
        ">
            <button
                id="btnAceptarLlamada"
                style="
                    width:70px;
                    height:70px;
                    border:none;
                    border-radius:50%;
                    background:#25d366;
                    color:#fff;
                    font-size:28px;
                    cursor:pointer;
                ">
                ✅
            </button>

            <button
                id="btnRechazarLlamada"
                style="
                    width:70px;
                    height:70px;
                    border:none;
                    border-radius:50%;
                    background:#ff2d55;
                    color:#fff;
                    font-size:28px;
                    cursor:pointer;
                ">
                ❌
            </button>
        </div>
    `;

    document.body.appendChild(div);

    document.getElementById(
        "btnAceptarLlamada"
    ).onclick =
    aceptarLlamada;

    document.getElementById(
        "btnRechazarLlamada"
    ).onclick =
    rechazarLlamada;
}

/* ===================================================== */
/* CREAR PEER */
/* ===================================================== */

function crearPeerConnection(){

    peerConnection =
    new RTCPeerConnection(rtcConfig);

    remoteStream =
    new MediaStream();

    remoteVideo.srcObject =
    remoteStream;

    peerConnection.ontrack =
    event => {

        event.streams[0]
        .getTracks()
        .forEach(track => {

            remoteStream.addTrack(track);
        });
    };

    peerConnection.onicecandidate =
    async event => {

        if(event.candidate){

            await callConnection.invoke(
                "EnviarIceCandidate",
                destino,
                JSON.stringify(
                    event.candidate
                )
            );
        }
    };
}

/* ===================================================== */
/* 📞 AUDIO */
/* ===================================================== */

async function llamarAudio(){

    try{

        await activarAudioSistema();

        currentCallType = "audio";

        crearUIllamada();

        localStream =
        await navigator.mediaDevices
        .getUserMedia({
            audio:true,
            video:false
        });

        localVideo.srcObject =
        localStream;

        crearPeerConnection();

        localStream.getTracks()
        .forEach(track => {

            peerConnection.addTrack(
                track,
                localStream
            );
        });

        const offer =
        await peerConnection.createOffer();

        await peerConnection
        .setLocalDescription(
            offer
        );

        await callConnection.invoke(
            "EnviarOferta",
            destino,
            JSON.stringify(offer),
            "audio"
        );

    }catch(error){

        console.error(
            "Error audio:",
            error
        );
    }
}

/* ===================================================== */
/* 🎥 VIDEO */
/* ===================================================== */

async function llamarVideo(){

    try{

        await activarAudioSistema();

        currentCallType = "video";

        crearUIllamada();

        localStream =
        await navigator.mediaDevices
        .getUserMedia({
            audio:true,
            video:true
        });

        localVideo.srcObject =
        localStream;

        crearPeerConnection();

        localStream.getTracks()
        .forEach(track => {

            peerConnection.addTrack(
                track,
                localStream
            );
        });

        const offer =
        await peerConnection.createOffer();

        await peerConnection
        .setLocalDescription(
            offer
        );

        await callConnection.invoke(
            "EnviarOferta",
            destino,
            JSON.stringify(offer),
            "video"
        );

    }catch(error){

        console.error(
            "Error video:",
            error
        );
    }
}

/* ===================================================== */
/* RECIBIR OFERTA */
/* ===================================================== */

callConnection.on(
    "RecibirOferta",
    async (
        oferta,
        tipo,
        remitente
    ) => {

        llamadaPendiente = {
            oferta,
            tipo,
            remitente
        };

        mostrarLlamadaEntrante(
            remitente,
            tipo
        );
    }
);

/* ===================================================== */
/* ACEPTAR */
/* ===================================================== */

async function aceptarLlamada(){

    try{

        detenerTono();

        document.getElementById(
            "incomingCall"
        )?.remove();

        const {
            oferta,
            tipo,
            remitente
        } = llamadaPendiente;

        currentCallType = tipo;

        crearUIllamada();

        localStream =
        await navigator.mediaDevices
        .getUserMedia({

            audio:true,

            video:
            tipo === "video"
        });

        localVideo.srcObject =
        localStream;

        crearPeerConnection();

        localStream.getTracks()
        .forEach(track => {

            peerConnection.addTrack(
                track,
                localStream
            );
        });

        await peerConnection
        .setRemoteDescription(
            new RTCSessionDescription(
                JSON.parse(oferta)
            )
        );

        const answer =
        await peerConnection
        .createAnswer();

        await peerConnection
        .setLocalDescription(
            answer
        );

        await callConnection.invoke(
            "EnviarRespuesta",
            remitente,
            JSON.stringify(answer)
        );

    }catch(error){

        console.error(error);
    }
}

/* ===================================================== */
/* RECHAZAR */
/* ===================================================== */

function rechazarLlamada(){

    detenerTono();

    llamadaPendiente = null;

    document.getElementById(
        "incomingCall"
    )?.remove();
}

/* ===================================================== */
/* RECIBIR RESPUESTA */
/* ===================================================== */

callConnection.on(
    "RecibirRespuesta",
    async respuesta => {

        await peerConnection
        .setRemoteDescription(
            new RTCSessionDescription(
                JSON.parse(respuesta)
            )
        );
    }
);

/* ===================================================== */
/* ICE */
/* ===================================================== */

callConnection.on(
    "RecibirIceCandidate",
    async candidate => {

        if(candidate){

            await peerConnection
            .addIceCandidate(
                new RTCIceCandidate(
                    JSON.parse(candidate)
                )
            );
        }
    }
);

/* ===================================================== */
/* FINALIZAR */
/* ===================================================== */

function finalizarLlamada(){

    detenerTono();

    if(localStream){

        localStream.getTracks()
        .forEach(track => {

            track.stop();
        });
    }

    if(peerConnection){

        peerConnection.close();
    }

    localStream = null;

    remoteStream = null;

    peerConnection = null;

    llamadaPendiente = null;

    document.getElementById(
        "callOverlay"
    )?.remove();

    document.getElementById(
        "incomingCall"
    )?.remove();
}

/* ===================================================== */
/* 🌸 EXPONER FUNCIONES GLOBALMENTE */
/* ===================================================== */

window.llamarAudio = llamarAudio;

window.llamarVideo = llamarVideo;

window.finalizarLlamada = finalizarLlamada;

/* ===================================================== */
/* DEBUG */
/* ===================================================== */

console.log("✅ calls.js cargado correctamente");