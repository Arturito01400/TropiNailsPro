namespace TropiNailsPro.Models
{
    public class Chat
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }   // quien envía (clienta o manicurista)
        public int ReceptorId { get; set; }  // quien recibe

        public string Mensaje { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public bool Leido { get; set; } = false;
    }
}