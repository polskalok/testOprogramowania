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
            // Arrange - Dane z dokumentu testera
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

            // Act
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            // Assert
            Assert.True(czyPoprawne); // Oczekujemy, że system zaakceptuje te dane
            Assert.Empty(wyniki); // Brak błędów
        }

        [Fact]
        public void TC_2_BrakWymaganegoPolaNazwisko_ZwracaBlad()
        {
            // Arrange - Dane z pominięciem nazwiska
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "", // Puste pole!
                Adres = "ul. Testowa 12, 00-000 Warszawa",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test.pl",
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            // Act
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            // Assert
            Assert.False(czyPoprawne); // Oczekujemy, że system odrzuci te dane
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Nazwisko")); // System musi wskazać na Nazwisko
        }

        [Fact]
        public void TC_9_ZlyFormatEmail_ZwracaBlad()
        {
            // Arrange - E-mail bez domeny zgodnie z dokumentem
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

            // Act
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            // Assert
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_10_EmailZaDlugi_ZwracaBlad()
        {
            // Arrange - Tworzymy bardzo długi adres email (> 255 znaków)
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

            // Act
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            // Assert
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_11_NiepoprawnyTelefon_ZwracaBlad()
        {
            // Arrange - Telefon ma 8 cyfr zamiast 9
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "Haslo1!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Plec = "M",
                Email = "jan.kowalski@test.pl",
                Telefon = "12345678" // <--- BŁĘDNY TELEFON
            };
            var kontekst = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            // Act
            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekst, wyniki, true);

            // Assert
            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Telefon"));
        }
    }
}