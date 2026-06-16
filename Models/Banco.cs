namespace TropiNailsPro.Models
{
    public class Banco
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty; // evita warnings de null
        public string LogoUrl { get; set; } = string.Empty; // evita warnings de null
    }
}
