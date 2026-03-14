using System;
using System.ComponentModel.DataAnnotations;

namespace przychodnia.Models
{
    public class Uzytkownik
    {
        [Key]
        public int Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Haslo { get; set; } = string.Empty;

        public string Imie { get; set; } = string.Empty;

        public string Nazwisko { get; set; } = string.Empty;

        public string Adres { get; set; } = string.Empty;

        public string Pesel { get; set; } = string.Empty;

        // Data urodzenia będzie wyliczana z PESEL-u (zrobimy to w logice dodawania)
        public DateTime DataUrodzenia { get; set; }

        public string Plec { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefon { get; set; } = string.Empty;

        // 0 - zwykły użytkownik, 1 - admin, 2 - pracownik
        public int Permisje { get; set; }

        // Flaga dla funkcji "Zapomnij" - jeśli false, użytkownik nie może się zalogować
        public bool CzyAktywny { get; set; } = true;
    }
}