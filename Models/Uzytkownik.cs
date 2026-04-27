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

        public DateTime? DataUrodzenia { get; set; } // [cite: 139]
        public string? Plec { get; set; } = string.Empty; //

        [Required(ErrorMessage = "E-mail jest wymagany")]
        [StringLength(255, ErrorMessage = "E-mail nie może przekroczyć 255 znaków")]
        [EmailAddress(ErrorMessage = "Niepoprawny format e-mailu")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", ErrorMessage = "Niepoprawny format e-mailu")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression("^\\d{9}$", ErrorMessage = "Numer telefonu musi zawierać dokładnie 9 cyfr")]
        public string Telefon { get; set; } = string.Empty;

        // Permisje przechowywane jako maska bitowa: Admin=1, Pracownik=2, Pacjent=4
        public int Permisje { get; set; }
        public bool CzyAktywny { get; set; } = true;

        
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        
        public bool MuszZmieniHaslo { get; set; } = false;

       
        public string? OstatniaHasla { get; set; } = string.Empty;
    }
}
