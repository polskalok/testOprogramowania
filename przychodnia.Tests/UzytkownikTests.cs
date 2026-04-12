using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using przychodnia.Models; 

namespace przychodnia.Tests.Models
{
    public class UzytkownikTests
    {
        [Fact]
        public void Walidacja_ZlyFormatEmaila_PowinnaZwrocicBlad()
        {
            
            var uzytkownik = new Uzytkownik { Email = "to_jest_zly_email.com" };
            var kontekstWalidacji = new ValidationContext(uzytkownik) { MemberName = "Email" };
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateProperty(uzytkownik.Email, kontekstWalidacji, wyniki);

           
            Assert.False(czyPoprawne); 
        }

        [Fact]
        public void Walidacja_ZbytKrotkiPesel_PowinnaZwrocicBlad()
        {
            
            var uzytkownik = new Uzytkownik { Pesel = "12345" };
            var kontekstWalidacji = new ValidationContext(uzytkownik) { MemberName = "Pesel" };
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateProperty(uzytkownik.Pesel, kontekstWalidacji, wyniki);

            
            Assert.False(czyPoprawne);
        }
    }
}