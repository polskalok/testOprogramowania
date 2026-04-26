using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using przychodnia.Models;

namespace przychodnia.Tests.Scenariusze
{
    public class DodanieUzytkownikaTests
    {
        [Fact]
        public void TC_1_DodanieUzytkownika_PoprawnyScenariusz()
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
        public void TC_2_DodanieUzytkownika_BrakWymaganegoPolaNazwisko()
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
        public void TC_9_DodanieUzytkownika_NiepoprawnyFormatEmail_BrakDomeny()
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
        public void TC_10_DodanieUzytkownika_NiepoprawnyFormatEmail_BrakMalpki()
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
                Email = "jan.kowalski.test.pl",
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_11_DodanieUzytkownika_NiepoprawnyFormatEmail_DwieMalpki()
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
                Email = "jan.kowalski@@test.pl",
                Telefon = "123456789"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Email"));
        }

        [Fact]
        public void TC_12_DodanieUzytkownika_EmailZaDlugi()
        {
            string zaDlugiEmail = new string('a', 250) + "@test.pl";
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "TestoweHaslo123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Adres = "ul. Testowa 12, 00-000 Warszawa",
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
        public void TC_13_DodanieUzytkownika_NiepoprawnyTelefon_ZbytKrotki()
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
                Telefon = "12345678"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Telefon"));
        }

        [Fact]
        public void TC_14_DodanieUzytkownika_NiepoprawnyTelefon_ZbytDlugi()
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
                Telefon = "1234567890"
            };
            var kontekstWalidacji = new ValidationContext(uzytkownik);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Telefon"));
        }
    }
}