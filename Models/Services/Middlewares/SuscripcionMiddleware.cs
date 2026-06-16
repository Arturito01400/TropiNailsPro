using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Middlewares
{
    public class SuscripcionMiddleware
    {
        private readonly RequestDelegate _next;

        public SuscripcionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            try
            {
                var path = context.Request.Path.Value ?? "";

                // =====================================================
                // 🔥 RUTAS PUBLICAS
                // =====================================================

                if (
                    path.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Registro", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Paypal", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Suscripcion/Pagar", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Suscripcion/Vencida", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Suscripcion/Renovar", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Suscripcion/Exito", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/Suscripcion/Cancelado", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                )
                {
                    await _next(context);
                    return;
                }

                // =====================================================
                // 🔥 OBTENER SESSION
                // =====================================================

                int? usuarioId = context.Session.GetInt32("UsuarioId");

                var rol = context.Session.GetString("Rol");

                // =====================================================
                // 🔥 SI NO HAY ROL -> LOGIN
                // =====================================================

                if (string.IsNullOrEmpty(rol))
                {
                    context.Response.Redirect("/Auth/Login");
                    return;
                }

                // =====================================================
                // 🔥 CLIENTAS GRATIS
                // =====================================================

                if (rol == "Clienta")
                {
                    await _next(context);
                    return;
                }

                // =====================================================
                // 🔥 SOLO MANICURISTAS PAGAN
                // =====================================================

                if (rol != "Manicurista")
                {
                    await _next(context);
                    return;
                }

                // =====================================================
                // 🔥 RESTAURAR SESSION SI SE PIERDE
                // =====================================================

                if (!usuarioId.HasValue)
                {
                    var email = context.User?.Identity?.Name;

                    if (!string.IsNullOrEmpty(email))
                    {
                        var usuario = await db.Manicuristas
                            .FirstOrDefaultAsync(x => x.Email == email);

                        if (usuario != null)
                        {
                            usuarioId = usuario.Id;

                            context.Session.SetInt32(
                                "UsuarioId",
                                usuario.Id
                            );

                            context.Session.SetString(
                                "Rol",
                                "Manicurista"
                            );
                        }
                    }
                }

                // =====================================================
                // 🔥 SI NO HAY SESSION
                // =====================================================

                if (!usuarioId.HasValue)
                {
                    context.Response.Redirect("/Auth/Login");
                    return;
                }

                // =====================================================
                // 🔥 BUSCAR SUSCRIPCION
                // =====================================================

                var manicurista = await db.Manicuristas
    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId.Value);

if (manicurista == null)
{
    context.Response.Redirect("/Auth/Login");
    return;
}

var suscripcion = await db.Suscripciones
    .Where(s => s.ManicuristaId == manicurista.Id)
    .OrderByDescending(s => s.FechaInicio)
    .FirstOrDefaultAsync();

                // =====================================================
                // 🔥 SI NO TIENE SUSCRIPCION
                // 🔥 CREAR PRUEBA GRATIS AUTOMATICA
                // =====================================================

                if (suscripcion == null)
                {
                    suscripcion = new Models.Suscripcion
                    {
                        ManicuristaId = manicurista.Id,

                        FechaInicio = DateTime.UtcNow,

                        // 🔥 15 DIAS GRATIS
                        FechaVencimiento = DateTime.UtcNow.AddDays(15),

                        Plan = "Prueba Gratis",

                        Activa = true,

                        Cancelada = false,

                        MetodoPago = "Trial",

                        EstadoPago = "TRIAL",

                        Monto = 0,

                        Moneda = "USD"
                    };

                    db.Suscripciones.Add(suscripcion);

                    await db.SaveChangesAsync();

                    await _next(context);
                    return;
                }

                // =====================================================
                // 🔥 VALIDAR SUSCRIPCION
                // =====================================================

                var ahora = DateTime.UtcNow;

                bool suscripcionValida =
                    suscripcion.Activa &&
                    !suscripcion.Cancelada &&
                    suscripcion.FechaVencimiento > ahora;

                // =====================================================
                // 🔥 BLOQUEAR SI VENCIO
                // =====================================================

                if (!suscripcionValida)
                {
                    context.Response.Redirect("/Suscripcion/Vencida");
                    return;
                }

                // =====================================================
                // 🔥 DISPONIBLE GLOBALMENTE
                // =====================================================

                context.Items["SuscripcionActiva"] = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "ERROR MIDDLEWARE: " + ex.Message
                );

                context.Response.Redirect("/Auth/Login");
                return;
            }

            await _next(context);
        }
    }
}