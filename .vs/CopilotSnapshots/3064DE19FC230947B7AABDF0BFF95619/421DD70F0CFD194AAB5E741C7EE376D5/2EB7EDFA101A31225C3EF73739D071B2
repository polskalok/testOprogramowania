using System;
using System.ComponentModel.DataAnnotations;

namespace przychodnia.Models
{
    public class Uzytkownik
    {
        [Key]
        public int ID { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Haslo { get; set; } = string.Empty; // SHA256
        public string Imie{ get; set; } = string.Empty;
        public string Nazwisko { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string Pesel { get; set; } = string.Empty;
        public DateTime DataUrodzenia { get; set; }
        public string Plec { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public int Permisje { get; set; } // 0-User, 1-Admin, 2-Pracownik
        public bool CzyAktywny { get; set; } = true;
    }
}