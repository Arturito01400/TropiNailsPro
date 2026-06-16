using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class CambiarClaveViewModel
    {
        [Required(ErrorMessage = "La clave actual es obligatoria")]
        [DataType(DataType.Password)]
        public string ClaveActual { get; set; } = string.Empty; // Inicializado para evitar advertencia

        [Required(ErrorMessage = "La nueva clave es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La nueva clave debe tener al menos 6 caracteres")]
        public string NuevaClave { get; set; } = string.Empty; // Inicializado para evitar advertencia

        [Required(ErrorMessage = "Debe confirmar la nueva clave")]
        [DataType(DataType.Password)]
        [Compare("NuevaClave", ErrorMessage = "La confirmación no coincide con la nueva clave")]
        public string ConfirmarClave { get; set; } = string.Empty; // Inicializado para evitar advertencia
    }
}