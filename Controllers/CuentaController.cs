using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class CuentaController : Controller
    {
        private readonly EmailService _correo;

        // ✅ Inyectamos EmailService
        public CuentaController(EmailService correo)
        {
            _correo = correo;
        }

        // GET: /Cuenta/Recuperar
        public IActionResult Recuperar()
        {
            return View();
        }

        // POST: /Cuenta/Recuperar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recuperar(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Por favor ingresa tu correo electrónico.";
                return View();
            }

            // ✅ Crear link hacia Reset
            var link = Url.Action(
                "Reset",
                "Account",   // tu controlador de reset
                new { email = email },
                Request.Scheme
            );

            // ✅ Cuerpo del correo (HTML moderno)
            string cuerpo = $@"
                <div style='font-family:Poppins,Arial;padding:20px'>
                    <h2 style='color:#1e90ff'>TropiNails Pro</h2>
                    <p>Haz clic en el botón para restablecer tu contraseña:</p>

                    <a href='{link}'
                       style='background:#1e90ff;color:white;
                              padding:12px 20px;
                              border-radius:10px;
                              text-decoration:none;
                              font-weight:bold'>
                       Restablecer contraseña
                    </a>

                    <p style='margin-top:20px;font-size:12px'>
                        Si no solicitaste esto, ignora el mensaje.
                    </p>
                </div>
            ";

            // ✅ Enviar correo REAL
            await _correo.EnviarCorreoAsync(
                email,
                "Recuperar contraseña - TropiNails Pro",
                cuerpo
            );

            ViewBag.Mensaje = $"Se envió un enlace de recuperación a {email}.";
            return View();
        }
    }
}
