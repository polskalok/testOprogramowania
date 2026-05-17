using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace przychodnia.Models
{
    public class Wizyta
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Wypełnij to pole")]
        public int PacjentID { get; set; }

        [ForeignKey("PacjentID")]
        public virtual Pacjent? Pacjent { get; set; }

        [Required(ErrorMessage = "Wypełnij to pole")]
        public int LekarzID { get; set; }

        [ForeignKey("LekarzID")]
        public virtual Uzytkownik? Lekarz { get; set; }

        [Required(ErrorMessage = "Wypełnij to pole")]
        public int GabinetID { get; set; }

        [ForeignKey("GabinetID")]
        public virtual Gabinet? Gabinet { get; set; }

        [Required(ErrorMessage = "Wypełnij to pole")]
        [DataType(DataType.DateTime)]
        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; } // Zawsze DataRozpoczecia + 30 minut

        [Required]
        public string Status { get; set; } = "Zarejestrowana"; // Zarejestrowana lub Zrealizowana


        
        public string? OpisDoleglywosci { get; set; } = string.Empty;

        public string? Zalecenia { get; set; } = string.Empty;

        public string? PrzepisaneLeki { get; set; } = string.Empty;
    }
}