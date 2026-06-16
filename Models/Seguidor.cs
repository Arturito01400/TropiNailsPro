using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Seguidor
    {
        [Key]
        public int Id { get; set; }

        public int SeguidorId { get; set; }
        public int SeguidoId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        // quien sigue
        [ForeignKey(nameof(SeguidorId))]
        [InverseProperty(nameof(Usuario.Siguiendo))]
        public virtual Usuario SeguidorUsuario { get; set; } = null!;

        // a quien sigue
        [ForeignKey(nameof(SeguidoId))]
        [InverseProperty(nameof(Usuario.Seguidores))]
        public virtual Usuario SeguidoUsuario { get; set; } = null!;
    }
}