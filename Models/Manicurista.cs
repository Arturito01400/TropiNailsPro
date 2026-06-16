using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Manicurista
    {
        public int Id { get; set; }

        // 🔥 RELACIÓN 1 A 1 CON USUARIO (CLAVE)
        public int UsuarioId { get; set; }

[ForeignKey("UsuarioId")]
public Usuario Usuario { get; set; } = null!;

        [Required, MaxLength(150)]
        public string NombreNegocio { get; set; } = "";

        public string Nombre => NombreNegocio;

        [NotMapped] 
        public string NombreUsuario => NombreNegocio;

        // ⚠️ ESTO YA EXISTE EN USUARIO → LO DEJAMOS SOLO POR COMPATIBILIDAD
        [NotMapped]
        public string Email => Usuario.Email ?? "";

        [NotMapped]
        public string PasswordHash => Usuario.Clave;

        [NotMapped]
        public string Password
        {
            get => Usuario.Clave;
            set => Usuario.Clave = value;
        }

        [Required]
        public string Plan { get; set; } = "Basico";

        public DateTime FechaInicioPrueba { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public bool Activa { get; set; }

        public string CodigoReferencia { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);

        // 🔥 ESTE ES EL QUE VAMOS A USAR PARA EL LINK
        [Required, MaxLength(20)]
        public string CodigoPublico { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10);

        [MaxLength(100)]
        public string? Slug { get; set; }

        public ICollection<SuscripcionPago>? Pagos { get; set; }

        public ICollection<Cliente>? Clientes { get; set; }

        public virtual ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
    }
}