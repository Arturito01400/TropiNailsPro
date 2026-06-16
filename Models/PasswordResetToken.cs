using System;

namespace TropiNailsPro.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime Expira { get; set; }

        public bool Usado { get; set; } = false;
    }
}
