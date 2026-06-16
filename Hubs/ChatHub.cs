using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        private static ConcurrentDictionary<
            string,
            ConcurrentDictionary<string, byte>
        > UsuariosConectados = new();

        private static ConcurrentDictionary<string, byte>
            LlamadasTerminadas = new();

        public ChatHub(
            AppDbContext context
        )
        {
            _context = context;
        }

        // ==========================================
        // CONECTAR
        // ==========================================
        public override async Task OnConnectedAsync()
        {
            var usuario =
                Context?.User?.Identity?.Name;

            // 🔥 FIX SESSION NULL
            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                !string.IsNullOrWhiteSpace(usuario)
            )
            {
                var conexiones =
                    UsuariosConectados.GetOrAdd(
                        usuario,
                        _ => new ConcurrentDictionary<string, byte>()
                    );

                conexiones.TryAdd(
                    Context.ConnectionId,
                    0
                );

                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    usuario
                );

                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    "usuarios_conectados"
                );

                await Clients.Group(
                    "usuarios_conectados"
                )
                .SendAsync(
                    "UsuarioConectado",
                    usuario
                );

                await Clients.All.SendAsync(
                    "UsuarioOnline",
                    usuario
                );
            }

            await base.OnConnectedAsync();
        }

        // ==========================================
        // DESCONECTAR
        // ==========================================
        public override async Task OnDisconnectedAsync(
            Exception? exception
        )
        {
            var usuario =
                Context?.User?.Identity?.Name;

            // 🔥 FIX SESSION NULL
            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                !string.IsNullOrWhiteSpace(usuario)
            )
            {
                if (
                    UsuariosConectados.TryGetValue(
                        usuario,
                        out var conexiones
                    )
                )
                {
                    conexiones.TryRemove(
                        Context.ConnectionId,
                        out _
                    );

                    if (
                        conexiones.IsEmpty
                    )
                    {
                        UsuariosConectados.TryRemove(
                            usuario,
                            out _
                        );

                        await Clients.All.SendAsync(
                            "UsuarioOffline",
                            usuario
                        );
                    }
                }

                await Clients.Group(
                    "usuarios_conectados"
                )
                .SendAsync(
                    "UsuarioDesconectado",
                    usuario
                );
            }

            await base.OnDisconnectedAsync(
                exception
            );
        }

        // ==========================================
        // MENSAJES
        // ==========================================
        public async Task EnviarMensaje(
            string destinatario,
            string contenido,
            string tipo = "texto"
        )
        {
            var remitente =
                Context?.User?.Identity?.Name;

            // 🔥 FIX SESSION NULL
            if (
                string.IsNullOrWhiteSpace(remitente)
            )
            {
                remitente =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(remitente)
                ||
                string.IsNullOrWhiteSpace(destinatario)
                ||
                string.IsNullOrWhiteSpace(contenido)
            )
                return;

            var mensaje =
                new Mensaje
                {
                    Remitente =
                        remitente,

                    Destinatario =
                        destinatario,

                    Contenido =
                        contenido.Trim(),

                    Tipo =
                        tipo,

                    Fecha =
                        DateTime.Now,

                    Estado =
                        "enviado",

                    Leido =
                        false
                };

            _context.Mensajes.Add(
                mensaje
            );

            await _context.SaveChangesAsync();

            var data =
                new
                {
                    id =
                        mensaje.Id,

                    remitente,

                    destinatario,

                    contenido =
                        mensaje.Contenido,

                    tipo =
                        mensaje.Tipo,

                    fecha =
                        mensaje.Fecha,

                    estado =
                        "entregado"
                };

            // 🔥 FIX TIEMPO REAL
            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "RecibirMensaje",
                data
            );

            await Clients.Group(
                remitente
            )
            .SendAsync(
                "RecibirMensaje",
                data
            );
        }

        // ==========================================
        // EDITAR
        // ==========================================
        public async Task EditarMensaje(
            int mensajeId,
            string nuevoTexto
        )
        {
            var usuario =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            var mensaje =
                await _context.Mensajes.FindAsync(
                    mensajeId
                );

            if (
                mensaje == null
                ||
                mensaje.Remitente != usuario
            )
                return;

            mensaje.Contenido =
                nuevoTexto;

            mensaje.Estado =
                "editado";

            await _context.SaveChangesAsync();

            await Clients.Group(
                mensaje.Destinatario
            )
            .SendAsync(
                "MensajeEditado",
                mensajeId,
                nuevoTexto
            );

            await Clients.Group(
                usuario
            )
            .SendAsync(
                "MensajeEditado",
                mensajeId,
                nuevoTexto
            );
        }

        // ==========================================
        // ELIMINAR PARA TODOS
        // ==========================================
        public async Task EliminarMensaje(
            int mensajeId
        )
        {
            var usuario =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            var mensaje =
                await _context.Mensajes.FindAsync(
                    mensajeId
                );

            if (
                mensaje == null
                ||
                mensaje.Remitente != usuario
            )
                return;

            mensaje.Contenido =
                "🚫 Este mensaje fue eliminado";

            mensaje.Tipo =
                "eliminado";

            mensaje.Estado =
                "eliminado";

            await _context.SaveChangesAsync();

            await Clients.Group(
                mensaje.Destinatario
            )
            .SendAsync(
                "MensajeEliminado",
                mensajeId
            );

            await Clients.Group(
                usuario
            )
            .SendAsync(
                "MensajeEliminado",
                mensajeId
            );
        }

        // ==========================================
        // ELIMINAR SOLO PARA MI
        // ==========================================
        public async Task EliminarMensajeSoloParaMi(
            int mensajeId
        )
        {
            var usuario =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(usuario)
            )
                return;

            var existe =
                await _context
                .MensajesEliminadosUsuarios
                .AnyAsync(
                    x =>
                    x.MensajeId == mensajeId
                    &&
                    x.Usuario == usuario
                );

            if (existe)
                return;

            _context
            .MensajesEliminadosUsuarios
            .Add(
                new MensajeEliminadoUsuario
                {
                    MensajeId =
                        mensajeId,

                    Usuario =
                        usuario
                }
            );

            await _context.SaveChangesAsync();

            await Clients.Caller.SendAsync(
                "MensajeEliminadoSoloParaMi",
                mensajeId
            );
        }

        // ==========================================
        // ALIAS JS
        // ==========================================
        public Task EliminarSoloParaMi(
            int mensajeId
        )
        {
            return EliminarMensajeSoloParaMi(
                mensajeId
            );
        }

        // ==========================================
        // TYPING
        // ==========================================
        public async Task Typing(
            string destinatario
        )
        {
            var usuario =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(usuario)
            )
            {
                usuario =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(usuario)
                ||
                string.IsNullOrWhiteSpace(destinatario)
            )
                return;

            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "MostrarTyping",
                usuario
            );
        }

        // ==========================================
        // 📞 WEBRTC OFERTA
        // ==========================================
        public async Task EnviarOferta(
            string destinatario,
            string oferta,
            string tipo
        )
        {
            var remitente =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(remitente)
            )
            {
                remitente =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(remitente)
                ||
                string.IsNullOrWhiteSpace(destinatario)
            )
                return;

            Console.WriteLine(
                $"📞 Oferta enviada de {remitente} a {destinatario}"
            );

            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "RecibirOferta",
                oferta,
                tipo,
                remitente
            );
        }

        // ==========================================
        // 📞 RESPUESTA
        // ==========================================
        public async Task EnviarRespuesta(
            string destinatario,
            string respuesta
        )
        {
            var remitente =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(remitente)
            )
            {
                remitente =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(remitente)
                ||
                string.IsNullOrWhiteSpace(destinatario)
            )
                return;

            Console.WriteLine(
                $"✅ Respuesta enviada de {remitente} a {destinatario}"
            );

            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "RecibirRespuesta",
                respuesta
            );
        }

        // ==========================================
        // 📞 ICE CANDIDATE
        // ==========================================
        public async Task EnviarIceCandidate(
            string destinatario,
            string candidate
        )
        {
            var remitente =
                Context?.User?.Identity?.Name;

            if (
                string.IsNullOrWhiteSpace(remitente)
            )
            {
                remitente =
                    Context?.GetHttpContext()?
                    .Session
                    .GetString("UsuarioNombre");
            }

            if (
                string.IsNullOrWhiteSpace(remitente)
                ||
                string.IsNullOrWhiteSpace(destinatario)
            )
                return;

            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "RecibirIceCandidate",
                candidate
            );
        }

        // ==========================================
        // FINALIZAR LLAMADA
        // ==========================================
        public async Task FinalizarLlamada(
            string destinatario
        )
        {
            if (
                string.IsNullOrWhiteSpace(destinatario)
            )
                return;

            await Clients.Group(
                destinatario
            )
            .SendAsync(
                "LlamadaFinalizada"
            );
        }
    }
}