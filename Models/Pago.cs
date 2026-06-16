using System;
using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class Pago
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre del cliente no puede superar 100 caracteres")]
        public string ClienteNombre { get; set; }

        [Required(ErrorMessage = "El modelo de uñas es obligatorio")]
        [StringLength(50, ErrorMessage = "El modelo no puede superar 50 caracteres")]
        public string ModeloUnas { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0, 1000000, ErrorMessage = "El monto debe ser mayor o igual a 0")]
        public decimal Monto { get; set; }

        [StringLength(100, ErrorMessage = "La referencia no puede superar 100 caracteres")]
        public string TransaccionId { get; set; }

        [Display(Name = "Fecha de Pago")]
        [DataType(DataType.DateTime)]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Display(Name = "Pagado")]
        public bool Pagado { get; set; } = false; // FALSE = deuda pendiente, TRUE = pagado


        // 🔐 NUEVO CAMPO PARA SEGURIDAD (SEPARAR MANICURISTAS)
        public int UsuarioId { get; set; }

    }
}