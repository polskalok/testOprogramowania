using System.ComponentModel.DataAnnotations;

namespace przychodnia.Models
{
    public class Gabinet
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Wypełnij to pole")]
        public string Numer { get; set; } = string.Empty; // np. "Gabinet 104"

        [Required(ErrorMessage = "Wypełnij to pole")]
        public string Specjalnosc { get; set; } = string.Empty; // np. "Ogólny", "Kardiologiczny"
    }
}