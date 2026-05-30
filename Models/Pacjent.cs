using System;
using System.ComponentModel.DataAnnotations;

namespace przychodnia.Models
{
    public class Pacjent
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Proszę podać imię pacjenta.")]
        [RegularExpression(@"^[a-zA-Z\u0100-\u017F]+$", ErrorMessage = "Imię może zawierać tylko litery.")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Proszę podać nazwisko pacjenta.")]
        [RegularExpression(@"^[a-zA-Z\u0100-\u017F\s-]+$", ErrorMessage = "Nazwisko może zawierać tylko litery i myślnik.")]
        public string Nazwisko { get; set; } = string.Empty;


        [Required(ErrorMessage = "Pole wymagane")]
        public string Adres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Numer PESEL jest wymagany.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
        public string Pesel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Podany format adresu e-mail jest nieprawidłowy.")]
        [StringLength(255, ErrorMessage = "E-mail nie może być dłuższy niż 255 znaków.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Numer telefonu musi składać się z dokładnie 9 cyfr.")]
        public string Telefon { get; set; } = string.Empty;

        }
    }