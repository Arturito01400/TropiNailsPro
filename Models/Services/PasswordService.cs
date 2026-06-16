using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;

namespace TropiNailsPro.Services
{
    public static class PasswordService
    {
        // 🔐 Generar hash seguro
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

            string hashed = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: password!,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

            return Convert.ToBase64String(salt) + "." + hashed;
        }

        // 🔍 Verificar contraseña (protege contra valores nulos o mal formateados)
        public static bool Verify(string hashGuardado, string passwordIngresado)
        {
            if (string.IsNullOrEmpty(hashGuardado) || !hashGuardado.Contains("."))
                return false; // protege contra valores nulos o mal formateados

            var parts = hashGuardado.Split('.');

            if (parts.Length != 2)
                return false; // protege contra formatos inválidos

            try
            {
                var salt = Convert.FromBase64String(parts[0]);

                string hashNuevo = Convert.ToBase64String(
                    KeyDerivation.Pbkdf2(
                        password: passwordIngresado!,
                        salt: salt,
                        prf: KeyDerivationPrf.HMACSHA256,
                        iterationCount: 10000,
                        numBytesRequested: 256 / 8));

                return hashNuevo == parts[1];
            }
            catch
            {
                return false; // si salt o hash son inválidos
            }
        }

        // 🔑 Generar contraseña temporal segura para recuperar
        public static string GenerateSecureTempPassword(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%&*!?";
            var data = new byte[length];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }

            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }
    }
}