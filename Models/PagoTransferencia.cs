using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class PagoTransferencia
    {
        public int Id { get; set; }

        // ==========================================
        // RELACIÓN CON CUENTA BANCARIA
        // ==========================================

        [Required]
        [Display(Name = "Cuenta Bancaria")]
        public int CuentaBancariaId { get; set; }

        [ForeignKey("CuentaBancariaId")]
        public CuentaBancaria? CuentaBancaria { get; set; }

        // ==========================================
        // COMPROBANTE
        // ==========================================

        [Required]
        [Display(Name = "Comprobante")]
        public string ImagenComprobante { get; set; } = string.Empty;

        // ==========================================
        // FECHA DEL PAGO
        // ==========================================

        [Display(Name = "Fecha del Pago")]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        // ==========================================
        // CLIENTA
        // ==========================================

        [Display(Name = "Nombre de la Clienta")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Display(Name = "Foto de la Clienta")]
        public string ClienteFoto { get; set; } = "/img/user-default.png";

        // ==========================================
        // MANICURISTA
        // ==========================================

        public int ManicuristaId { get; set; }
    }
}