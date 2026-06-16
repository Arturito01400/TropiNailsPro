namespace TropiNailsPro.Models
{
    public class MensajeEliminadoUsuario
    {
        public int Id { get; set; }

        public int MensajeId { get; set; }

        public string Usuario { get; set; } = "";

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}