using System.Security.Cryptography;
using System.Text;

namespace przychodnia.Services 
{
    public static class PasswordHasher
    {
       
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Zamiana hasła na tablicę bajtów
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Konwersja bajtów na tekst w formacie szesnastkowym (hex)
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}