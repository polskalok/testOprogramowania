using System;
using System.ComponentModel.DataAnnotations;

namespace przychodnia.Models
{
    public class Uzytkownik
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Login jest wymagany")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        public string Haslo { get; set; } = string.Empty; // SHA256

        [Required(ErrorMessage = "Imię jest wymagane")]
        public string Imie{ get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string Nazwisko { get; set; } = string.Empty;

        public string Adres { get; set; } = string.Empty;

        [Required(ErrorMessage = "PESEL jest wymagany")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL musi mieć 11 cyfr")]
        public string Pesel { get; set; } = string.Empty;

        public DateTime DataUrodzenia { get; set; }

        public string Plec { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail jest wymagany")]
        [StringLength(255, ErrorMessage = "E-mail nie może przekroczyć 255 znaków")]
        [EmailAddress(ErrorMessage = "Niepoprawny format e-mailu")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression("^\\d{9}$", ErrorMessage = "Numer telefonu musi zawierać dokładnie 9 cyfr")]
        public string Telefon { get; set; } = string.Empty;

        public int Permisje { get; set; } // 0-User, 1-Admin, 2-Pracownik
        public bool CzyAktywny { get; set; } = true;
    }
}