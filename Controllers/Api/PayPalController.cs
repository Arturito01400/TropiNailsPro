using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers.Api
{
    [Route("api/paypal")]
    [ApiController]
    public class PayPalController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PayPalService _paypal;

        // Plan único mensual TropiNails Pro
// USD $10.20 ≈ RD$600
private const decimal MONTO_UNICO = 10.20m;
        private const string PLAN_UNICO = "Premium";

        public PayPalController(AppDbContext context, PayPalService paypal)
        {
            _context = context;
            _paypal = paypal;
        }

        [HttpPost("crear-orden")]
        public async Task<IActionResult> CrearOrden([FromBody] PagoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email requerido" });

            try
            {
                var orderId = await _paypal.CrearOrden(MONTO_UNICO, "Suscripción Premium");

                // 🔥 PROTECCIÓN CRÍTICA PARA EVITAR CARGA INFINITA EN PAYPAL
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return StatusCode(500, new
                    {
                        message = "Error generando orden PayPal",
                        ok = false
                    });
                }

                return Ok(new { id = orderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno creando orden PayPal",
                    error = ex.Message,
                    ok = false
                });
            }
        }

        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar([FromBody] PagoRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.OrderID) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { ok = false, message = "Datos inválidos" });
            }

            var pagoExistente = await _context.Pagos
                .AnyAsync(p => p.TransaccionId == request.OrderID);

            if (pagoExistente)
                return Ok(new { ok = true, message = "Pago ya procesado" });

            var manicurista = await _context.Manicuristas
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m =>
                    m.Usuario != null &&
                    m.Usuario.Email == request.Email);

            if (manicurista == null)
    return NotFound(new { ok = false, message = "Usuario no encontrado" });

var pagoValido =
    await _paypal.CapturarOrden(request.OrderID);

if (!pagoValido)
{
    return BadRequest(new
    {
        ok = false,
        message = "Pago PayPal inválido"
    });
}

var pago = new Pago
{
    ClienteNombre = manicurista.NombreNegocio ?? "Cliente",
    ModeloUnas = $"Suscripción {PLAN_UNICO}",
    Monto = MONTO_UNICO,
    TransaccionId = request.OrderID,
    FechaPago = DateTime.UtcNow,
    Pagado = true,
    UsuarioId = manicurista.Id
};

            _context.Pagos.Add(pago);

            var suscripcion = await _context.Suscripciones
                .Where(s => s.ManicuristaId == manicurista.Id)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            if (suscripcion == null)
            {
                suscripcion = new Suscripcion
                {
                    ManicuristaId = manicurista.Id
                };

                _context.Suscripciones.Add(suscripcion);
            }

            suscripcion.Plan = PLAN_UNICO;
            suscripcion.FechaInicio = DateTime.UtcNow;
            suscripcion.FechaVencimiento = DateTime.UtcNow.AddMonths(1);
            suscripcion.Activa = true;
            suscripcion.Cancelada = false;
            suscripcion.MetodoPago = "PayPal";
            suscripcion.EstadoPago = "ACTIVE";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                message = "Suscripción activada correctamente"
            });
        }
    }

    public class PagoRequest
    {
        public string? OrderID { get; set; }
        public string? Email { get; set; }
    }
}