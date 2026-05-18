using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace przychodnia.Models
{
    public class Wizyta
    {
        [Key]
        public int ID { get; set; }

      
        [Required(ErrorMessage = "Wybierz pacjenta z listy.")]
        public int? PacjentID { get; set; }

        [ForeignKey("PacjentID")]
        public virtual Pacjent? Pacjent { get; set; }

        
        [Required(ErrorMessage = "Wybór lekarza jest wymagany.")]
        public int? LekarzID { get; set; }

        [ForeignKey("LekarzID")]
        public virtual Uzytkownik? Lekarz { get; set; }

       
        [Required(ErrorMessage = "Wybór gabinetu jest wymagany.")]
        public int? GabinetID { get; set; }

        [ForeignKey("GabinetID")]
        public virtual Gabinet? Gabinet { get; set; }

        [Required(ErrorMessage = "Data i godzina są wymagane.")]
        [DataType(DataType.DateTime)]
        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; }

        [Required]
        public string Status { get; set; } = "Zarejestrowana";

        public string? OpisDoleglywosci { get; set; } = string.Empty;
        public string? Zalecenia { get; set; } = string.Empty;
        public string? PrzepisaneLeki { get; set; } = string.Empty;
    }
}