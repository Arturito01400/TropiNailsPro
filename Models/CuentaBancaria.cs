using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class CuentaBancaria
    {
        public int Id { get; set; }

        // ==========================================
        // RELACIÓN CON LA MANICURISTA
        // ==========================================

        [Required]
        public int ManicuristaId { get; set; }

        // ==========================================
        // DATOS DE LA CUENTA
        // ==========================================

        [Required(ErrorMessage = "El banco es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Banco")]
        public string Banco { get; set; } = string.Empty;

        [Required(ErrorMessage = "El titular es obligatorio")]
        [StringLength(150)]
        [Display(Name = "Titular de la cuenta")]
        public string Titular { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de cuenta es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Número de cuenta")]
        public string NumeroCuenta { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de cuenta es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Tipo de cuenta")]
        public string TipoCuenta { get; set; } = string.Empty;

        // ==========================================
        // 🔥 LOGO PERSONALIZADO (NUEVO - OPCIONAL)
        // ==========================================

        [Display(Name = "Logo del banco")]
        public string? LogoPersonalizado { get; set; }

        // ==========================================
        // RELACIÓN CON PAGOS POR TRANSFERENCIA
        // ==========================================

        public ICollection<PagoTransferencia>? PagosTransferencia { get; set; }
    }
}