using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class DisponibilidadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;


        public DisponibilidadesController(
            AppDbContext context,
            TimeService timeService)
        {
            _context = context;
            _timeService = timeService;
        }




        // LISTADO
        public async Task<IActionResult> Index()
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");


            var disponibilidades = await _context.Disponibilidades
                .Where(x => x.ManicuristaId == manicuristaId)
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .ToListAsync();


            return View(disponibilidades);
        }





        // CREAR
public IActionResult Create()
{
    return View(new Disponibilidad
    {
        Fecha = _timeService.ObtenerHoraLocal().Date,
        Hora = new TimeSpan(9, 0, 0),
        Disponible = true
    });
}





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Disponibilidad disponibilidad)
        {

            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");



            if (!ValidarHorario(disponibilidad))
            {
                return View(disponibilidad);
            }




            disponibilidad.ManicuristaId =
                manicuristaId.Value;



            disponibilidad.FechaRegistro =
                _timeService.ObtenerHoraLocal();




            _context.Add(disponibilidad);


            await _context.SaveChangesAsync();



            return RedirectToAction(nameof(Index));
        }








        // EDITAR

        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
                return NotFound();



            var disponibilidad =
                await _context.Disponibilidades
                .FindAsync(id);



            if (disponibilidad == null)
                return NotFound();



            return View(disponibilidad);
        }








        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Disponibilidad disponibilidad)
        {

            if (id != disponibilidad.Id)
                return NotFound();




            var manicuristaId =
                ObtenerManicuristaId();



            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");





            if (!ValidarHorario(disponibilidad))
            {
                return View(disponibilidad);
            }






            var disponibilidadBD =
                await _context.Disponibilidades
                .FirstOrDefaultAsync(x => x.Id == id);





            if (disponibilidadBD == null)
                return NotFound();






            disponibilidadBD.Fecha =
                disponibilidad.Fecha;



            disponibilidadBD.Hora =
                disponibilidad.Hora;



            disponibilidadBD.Nota =
                disponibilidad.Nota;



            disponibilidadBD.Disponible =
                disponibilidad.Disponible;




            // mantenemos el dueño real

            disponibilidadBD.ManicuristaId =
                manicuristaId.Value;





            await _context.SaveChangesAsync();




            return RedirectToAction(nameof(Index));
        }









        // ELIMINAR

        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null)
                return NotFound();




            var disponibilidad =
                await _context.Disponibilidades
                .FirstOrDefaultAsync(x => x.Id == id);




            if (disponibilidad == null)
                return NotFound();




            return View(disponibilidad);
        }






        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var disponibilidad =
                await _context.Disponibilidades
                .FindAsync(id);




            if (disponibilidad != null)
            {

                _context.Disponibilidades.Remove(disponibilidad);

                await _context.SaveChangesAsync();

            }



            return RedirectToAction(nameof(Index));
        }









        // ======================================================
        // VALIDACIÓN DE FECHA Y HORA
        // ======================================================

        private bool ValidarHorario(
            Disponibilidad disponibilidad)
        {


            var ahora =
                _timeService.ObtenerHoraLocal();




            // Fecha anterior a hoy

            if (disponibilidad.Fecha.Date < ahora.Date)
            {

                ModelState.AddModelError(
                    "Fecha",
                    "No puedes seleccionar una fecha pasada."
                );


                return false;

            }





            // Validar hora cuando es hoy

            if (disponibilidad.Fecha.Date == ahora.Date)
            {


                if (disponibilidad.Hora <= ahora.TimeOfDay)
                {

                    ModelState.AddModelError(
                        "Hora",
                        "No puedes seleccionar una hora que ya pasó."
                    );


                    return false;

                }

            }



            return true;

        }







        private int? ObtenerManicuristaId()
        {

            var claim =
                User.FindFirst("ManicuristaId");



            if (claim == null)
                return null;



            return int.Parse(claim.Value);

        }


    }

}