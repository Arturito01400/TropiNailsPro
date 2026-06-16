using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models.ViewModels
{
    public class CambiarContrasenaViewModel
    {
        // =====================================================
        // 🔐 TOKEN QUE VIENE DESDE EL CORREO
        // =====================================================

        public string Token { get; set; } = string.Empty;

        // =====================================================
        // 🔑 NUEVA CONTRASEÑA
        // =====================================================

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Debe tener al menos 6 caracteres")]
        public string NuevaClave { get; set; } = string.Empty;

        // =====================================================
        // 🔁 CONFIRMAR
        // =====================================================

        [Required(ErrorMessage = "Confirma la contraseña")]
        [DataType(DataType.Password)]
        [Compare("NuevaClave", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarClave { get; set; } = string.Empty;
    }
}