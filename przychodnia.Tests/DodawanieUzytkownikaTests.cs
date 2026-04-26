using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using przychodnia.Models;

namespace przychodnia.Tests.Scenariusze
{
    public class DodanieUzytkownikaTests
    {
        [Fact]
        public void TC_1_PrzechodziWalidacje()
        {
            
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Adres = "ul. Testowa 12, 00-000 Warszawa",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test.pl",
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            
            Assert.True(czyPoprawne); 
            Assert.Empty(wyniki); 
        }

        [Fact]
        public void TC_2_BrakWymaganegoPolaNazwisko_ZwracaBlad()
        {
            
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "", 
                Adres = "ul. Testowa 12, 00-000 Warszawa",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test.pl",
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            
            Assert.False(czyPoprawne); 
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Nazwisko")); 
        }

        [Fact]
        public void TC_9_ZlyFormatEmail_ZwracaBlad()
        {
            
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test", 
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_10_EmailZaDlugi_ZwracaBlad()
        {
            
            string zaDlugiEmail = new string('a', 250) + "@test.pl";

            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Plec = "M",
                Email = zaDlugiEmail,
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_11_NiepoprawnyTelefon_ZwracaBlad()
        {
            
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "Haslo1!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test.pl",
                Telefon = "12345678" 
            };
            var kontekst = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

           
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekst, wyniki, true);

            
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Telefon"));
        }
    }
}