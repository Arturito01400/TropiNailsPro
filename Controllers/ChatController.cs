using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;

using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;
using TropiNailsPro.Hubs;

namespace TropiNailsPro.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        private readonly NotificacionService _notificacionService;

        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            AppDbContext context,
            NotificacionService notificacionService,
            IHubContext<ChatHub> hubContext
        )
        {
            _context = context;

            _notificacionService = notificacionService;

            _hubContext = hubContext;
        }

        //===================================
        // CHAT VIEW
        //===================================
        public IActionResult Index(
            string usuario
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    usuario
                )
            )
            {
                return RedirectToAction(
                    "Conversaciones"
                );
            }

            ViewBag.UsuarioDestino =
                usuario;

            return View();
        }

        //===================================
        // HISTORIAL
        //===================================
        [HttpGet]
        public async Task<IActionResult>
        ObtenerMensajes(
            string conUsuario
        )
        {
            var yo =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            if (
                string.IsNullOrEmpty(yo)
            )
            {
                return Unauthorized();
            }

            if (
                string.IsNullOrWhiteSpace(
                    conUsuario
                )
            )
            {
                return BadRequest();
            }

            var eliminados =
                await _context.MensajesEliminadosUsuarios
                .Where(x => x.Usuario == yo)
                .Select(x => x.MensajeId)
                .ToListAsync();

            var mensajes =
                await _context.Mensajes
                .AsNoTracking()
                .Where(m =>

                    (
                        m.Remitente == yo
                        &&
                        m.Destinatario == conUsuario
                    )

                    ||

                    (
                        m.Remitente == conUsuario
                        &&
                        m.Destinatario == yo
                    )

                )
                .Where(m => !eliminados.Contains(m.Id))
                .OrderBy(x => x.Fecha)
                .ToListAsync();

            var mensajesProcesados =
                mensajes.Select(m => new
                {
                    id = m.Id,

                    remitente = m.Remitente,

                    destinatario = m.Destinatario,

                    contenido =
                        m.Tipo == "eliminado"
                        ?
                        "🚫 Este mensaje fue eliminado"
                        :
                        m.Contenido,

                    tipo = m.Tipo,

                    fecha = m.Fecha,

                    estado = m.Estado
                });

            return Json(
                new
                {
                    data = mensajesProcesados
                }
            );
        }

        //===================================
        // TEXTO
        //===================================
        [HttpPost]
        public async Task<IActionResult>
        Enviar(
            string destinatario,
            string contenido,
            string tipo = "texto"
        )
        {
            try
            {
                var remitente =
                    HttpContext.Session.GetString(
                        "UsuarioNombre"
                    );

                if (
                    string.IsNullOrWhiteSpace(remitente)
                    ||
                    string.IsNullOrWhiteSpace(destinatario)
                    ||
                    string.IsNullOrWhiteSpace(contenido)
                )
                {
                    return BadRequest();
                }

                var mensaje =
                    new Mensaje
                    {
                        Remitente = remitente,

                        Destinatario = destinatario,

                        Contenido = contenido.Trim(),

                        Tipo = tipo,

                        Fecha = DateTime.Now,

                        Estado = "enviado",

                        Leido = false
                    };

                await _context.Mensajes.AddAsync(
                    mensaje
                );

                await _context.SaveChangesAsync();

                // 🔥 SIGNALR
                await _hubContext.Clients.All.SendAsync(
                    "RecibirMensaje",
                    new
                    {
                        id = mensaje.Id,
                        remitente = mensaje.Remitente,
                        destinatario = mensaje.Destinatario,
                        contenido = mensaje.Contenido,
                        tipo = mensaje.Tipo,
                        fecha = mensaje.Fecha
                    }
                );

                await _notificacionService
                .EnviarNotificacionTiempoReal(
                    destinatario,
                    $"💬 {remitente} te escribió"
                );

                await _notificacionService
                .ActualizarContador(
                    destinatario,
                    1
                );

                return Ok(
                    new
                    {
                        ok = true,
                        mensaje = mensaje.Contenido,
                        fecha = mensaje.Fecha,
                        remitente = mensaje.Remitente
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    ex.Message
                );
            }
        }

        //===================================
        // SUBIR ARCHIVOS
        //===================================
        [HttpPost]
        public async Task<IActionResult>
        SubirArchivo(
            string destinatario,
            IFormFile archivo
        )
        {
            if (
                archivo == null
                || archivo.Length == 0
            )
            {
                return BadRequest(
                    "archivo inválido"
                );
            }

            var remitente =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            if (
                string.IsNullOrEmpty(
                    remitente
                )
            )
            {
                return Unauthorized();
            }

            var uploads =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads"
                );

            if (
                !Directory.Exists(
                    uploads
                )
            )
            {
                Directory.CreateDirectory(
                    uploads
                );
            }

            var fileName =
                $"{Guid.NewGuid()}_{archivo.FileName}";

            var filePath =
                Path.Combine(
                    uploads,
                    fileName
                );

            using (
                var stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                )
            )
            {
                await archivo.CopyToAsync(
                    stream
                );
            }

            string tipo =
                archivo.ContentType.StartsWith(
                    "image/"
                )
                ?
                "imagen"
                :
                "archivo";

            var ruta =
                "/uploads/" + fileName;

            var mensaje =
                new Mensaje
                {
                    Remitente = remitente,

                    Destinatario = destinatario,

                    Contenido = ruta,

                    Tipo = tipo,

                    Fecha = DateTime.Now,

                    Estado = "enviado",

                    Leido = false
                };

            await _context.Mensajes.AddAsync(
                mensaje
            );

            await _context.SaveChangesAsync();

            // 🔥 SIGNALR
            await _hubContext.Clients.All.SendAsync(
                "RecibirMensaje",
                new
                {
                    id = mensaje.Id,
                    remitente = mensaje.Remitente,
                    destinatario = mensaje.Destinatario,
                    contenido = mensaje.Contenido,
                    tipo = mensaje.Tipo,
                    fecha = mensaje.Fecha
                }
            );

            await _notificacionService
            .EnviarNotificacionTiempoReal(
                destinatario,
                $"📎 {remitente} envió archivo"
            );

            await _notificacionService
            .ActualizarContador(
                destinatario,
                1
            );

            return Json(
    new
    {
        id = mensaje.Id,
        url = ruta,
        tipo = tipo
    }
);
        

    }     
        //===================================
        // NOTA DE VOZ
        //===================================
        [HttpPost]
        public async Task<IActionResult>
        SubirNotaVoz(
            string destinatario,
            IFormFile audio
        )
        {
            if (
                audio == null
                || audio.Length == 0
            )
            {
                return BadRequest(
                    "audio inválido"
                );
            }

            var remitente =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            if (
                string.IsNullOrEmpty(
                    remitente
                )
            )
            {
                return Unauthorized();
            }

            var uploads =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads"
                );

            if (
                !Directory.Exists(
                    uploads
                )
            )
            {
                Directory.CreateDirectory(
                    uploads
                );
            }

            var fileName =
                $"{Guid.NewGuid()}_{audio.FileName}";

            var filePath =
                Path.Combine(
                    uploads,
                    fileName
                );

            using (
                var stream =
                new FileStream(
                    filePath,
                    FileMode.Create
                )
            )
            {
                await audio.CopyToAsync(
                    stream
                );
            }

            string ruta =
                "/uploads/" + fileName;

            var mensaje =
                new Mensaje
                {
                    Remitente = remitente,

                    Destinatario = destinatario,

                    Contenido = ruta,

                    Tipo = "audio",

                    Fecha = DateTime.Now,

                    Estado = "enviado",

                    Leido = false
                };

            await _context.Mensajes.AddAsync(
                mensaje
            );

            await _context.SaveChangesAsync();

            // 🔥 SIGNALR
            await _hubContext.Clients.All.SendAsync(
                "RecibirMensaje",
                new
                {
                    id = mensaje.Id,
                    remitente = mensaje.Remitente,
                    destinatario = mensaje.Destinatario,
                    contenido = mensaje.Contenido,
                    tipo = mensaje.Tipo,
                    fecha = mensaje.Fecha
                }
            );

            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    destinatario,
                    $"🎤 {remitente} envió audio"
                );

            await _notificacionService
                .ActualizarContador(
                    destinatario,
                    1
                );

            return Json(
    new
    {
        id = mensaje.Id,
        url = ruta
    }
);

        }
        [HttpPost]
        public Task<IActionResult>
        EnviarAudio(
            string destinatario,
            IFormFile audio
        )
        {
            return SubirNotaVoz(
                destinatario,
                audio
            );
        }

        //===================================
        // LISTADO VISTA
        //===================================
        [HttpGet]
        public async Task<IActionResult>
        Conversaciones()
        {
            var yo =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            if (
                string.IsNullOrEmpty(yo)
            )
            {
                return Unauthorized();
            }

            var eliminados =
                await _context.MensajesEliminadosUsuarios
                .Where(x => x.Usuario == yo)
                .Select(x => x.MensajeId)
                .ToListAsync();

            var conversaciones =
                await _context.Mensajes
                .Where(m =>

                    (
                        m.Remitente == yo
                        ||
                        m.Destinatario == yo
                    )

                    &&

                    !eliminados.Contains(m.Id)
                )
                .OrderByDescending(
                    x => x.Fecha
                )
                .ToListAsync();

            var lista =
                conversaciones
                .GroupBy(
                    m =>
                    m.Remitente == yo
                    ?
                    m.Destinatario
                    :
                    m.Remitente
                )
                .Select(g =>
                    new ChatConversacion
                    {
                        Usuario = g.Key,

                        UltimoMensaje =
                            g.First().Tipo == "eliminado"
                            ?
                            "🚫 Mensaje eliminado"
                            :
                            g.First().Contenido,

                        Fecha = g.First().Fecha
                    }
                )
                .ToList();

            return View(
                lista
            );
        }

        //===================================
        // LISTADO JSON
        //===================================
        [HttpGet]
        public async Task<IActionResult>
        ObtenerConversaciones()
        {
            var yo =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            if (
                string.IsNullOrEmpty(yo)
            )
            {
                return Unauthorized();
            }

            var eliminados =
                await _context.MensajesEliminadosUsuarios
                .Where(x => x.Usuario == yo)
                .Select(x => x.MensajeId)
                .ToListAsync();

            var conversaciones =
                await _context.Mensajes
                .Where(m =>

                    (
                        m.Remitente == yo
                        ||
                        m.Destinatario == yo
                    )

                    &&

                    !eliminados.Contains(m.Id)
                )
                .OrderByDescending(
                    x => x.Fecha
                )
                .ToListAsync();

            var lista =
                conversaciones
                .GroupBy(
                    m =>
                    m.Remitente == yo
                    ?
                    m.Destinatario
                    :
                    m.Remitente
                )
                .Select(g =>
                    new
                    {
                        usuario = g.Key,

                        ultimoMensaje =
                            g.First().Tipo == "eliminado"
                            ?
                            "🚫 Mensaje eliminado"
                            :
                            g.First().Contenido,

                        fecha = g.First().Fecha
                    }
                )
                .ToList();

            return Json(
                lista
            );
        }
    }
}