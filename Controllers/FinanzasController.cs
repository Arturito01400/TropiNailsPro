using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Services;
using TropiNailsPro.ViewModels;

namespace TropiNailsPro.Controllers
{
    public class FinanzasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;

        public FinanzasController(
            AppDbContext context,
            TimeService timeService)
        {
            _context = context;
            _timeService = timeService;
        }


        public async Task<IActionResult> ResumenMensual()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");


            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }



            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();



            if (manicuristaId == 0)
            {
                TempData["Error"] =
                    "No se encontró la información de la manicurista.";

                return RedirectToAction("Index", "Home");
            }



            var añoActual = _timeService
                .ObtenerHoraLocal()
                .Year;



            var pagos = await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId &&
                    p.FechaPago.Year == añoActual)
                .ToListAsync();



            var gastos = await _context.Gastos
                .Where(g =>
                    g.ManicuristaId == manicuristaId &&
                    g.FechaGasto.Year == añoActual)
                .ToListAsync();



            var gastosPersonales = await _context.GastosPersonales
                .Where(g =>
                    g.ManicuristaId == manicuristaId &&
                    g.FechaGasto.Year == añoActual)
                .ToListAsync();



            var meses = new List<ResumenMensualViewModel>();



            for (int mes = 1; mes <= 12; mes++)
            {

                var ingresosMes = pagos
                    .Where(p => p.FechaPago.Month == mes)
                    .Sum(p => p.Monto);



                var gastosNegocioMes = gastos
                    .Where(g => g.FechaGasto.Month == mes)
                    .Sum(g => g.Monto);



                var gastosPersonalesMes = gastosPersonales
                    .Where(g => g.FechaGasto.Month == mes)
                    .Sum(g => g.Monto);



                meses.Add(new ResumenMensualViewModel
                {

                    Año = añoActual,

                    Mes = mes,


                    NombreMes = new DateTime(
                        añoActual,
                        mes,
                        1
                    ).ToString("MMMM"),



                    TotalIngresos = ingresosMes,


                    TotalGastosNegocio = gastosNegocioMes,


                    TotalGastosPersonales = gastosPersonalesMes,



                    Ganancia =
                        ingresosMes
                        -
                        gastosNegocioMes
                        -
                        gastosPersonalesMes

                });

            }



            return View(meses);
        }
    }
}