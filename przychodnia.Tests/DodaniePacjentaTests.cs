using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using Xunit;

namespace przychodnia.Tests.Scenariusze
{
    public class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    public class DodaniePacjentaTests
    {
        [Fact]
        public void TC_17_01_RejestracjaPacjenta_SciezkaGlowna()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_17_01")
                .Options;

            using var context = new ApplicationDbContext(options);
            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };

            var result = controller.PracownikDodaj(pacjent);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PracownikLista", redirectResult.ActionName);
            Assert.NotNull(context.Pacjenci.FirstOrDefault(p => p.Pesel == "85010112345"));
        }

        [Fact]
        public void TC_17_02_RejestracjaPacjenta_BrakWymaganegoPolaImie()
        {
            var pacjent = new Pacjent
            {
                Imie = "",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage == "Proszę podać imię pacjenta.");
        }

        [Fact]
        public void TC_17_03_RejestracjaPacjenta_BrakWymaganegoPolaNazwisko()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage == "Proszę podać nazwisko pacjenta.");
        }

        [Fact]
        public void TC_17_04_RejestracjaPacjenta_BrakWymaganegoPolaPesel()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage == "Numer PESEL jest wymagany.");
        }

        [Fact]
        public void TC_17_05_RejestracjaPacjenta_BrakWymaganegoPolaAdres()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("wymagane"));
        }

        [Fact]
        public void TC_17_06_RejestracjaPacjenta_BrakWymaganegoPolaTelefon()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage == "Numer telefonu jest wymagany.");
        }

        [Fact]
        public void TC_17_07_RejestracjaPacjenta_BrakWymaganegoPolaEmail()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = ""
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage == "Adres e-mail jest wymagany.");
        }

        [Fact]
        public void TC_17_08_RejestracjaPacjenta_NiepoprawnyPesel_CyfraKontrolna()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_17_08")
                .Options;

            using var context = new ApplicationDbContext(options);
            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112340",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@test.pl"
            };

            var result = controller.PracownikDodaj(pacjent) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Contains(controller.ModelState["Pesel"].Errors, e => e.ErrorMessage.Contains("cyfra kontrolna"));
        }

        [Fact]
        public void TC_17_09_RejestracjaPacjenta_DuplikatPesel()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_17_09")
                .Options;

            using var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent
            {
                Imie = "Istniejacy",
                Nazwisko = "Pacjent",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "111222333",
                Email = "istniejacy@test.pl"
            });
            context.SaveChanges();

            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var pacjent = new Pacjent
            {
                Imie = "Adam",
                Nazwisko = "Nowak",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "adam.nowak@test.pl"
            };

            var result = controller.PracownikDodaj(pacjent) as ViewResult;

            Assert.Equal("Pacjent o tym numerze PESEL jest już w bazie.", controller.ViewBag.Error);
        }

        [Fact]
        public void TC_17_10_RejestracjaPacjenta_NiepoprawnyFormatEmail_BrakMalpy()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalskitest.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("format"));
        }

        [Fact]
        public void TC_17_11_RejestracjaPacjenta_NiepoprawnyFormatEmail_DwieMalpy()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "jan.kowalski@@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("format"));
        }

        [Fact]
        public void TC_17_12_RejestracjaPacjenta_EmailZaDlugi()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = new string('a', 250) + "@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("255 znaków"));
        }

        [Fact]
        public void TC_17_13_RejestracjaPacjenta_DuplikatEmail()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_17_13")
                .Options;

            using var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent
            {
                Imie = "Istniejacy",
                Nazwisko = "Pacjent",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "111222333",
                Email = "istniejacy@test.pl"
            });
            context.SaveChanges();

            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var pacjent = new Pacjent
            {
                Imie = "Adam",
                Nazwisko = "Nowak",
                Pesel = "90020212345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "123456789",
                Email = "istniejacy@test.pl"
            };

            var result = controller.PracownikDodaj(pacjent) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public void TC_17_14_RejestracjaPacjenta_NiepoprawnyNumerTelefonu_ZbytKrotki()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "12345678",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("9 cyfr"));
        }

        [Fact]
        public void TC_17_15_RejestracjaPacjenta_NiepoprawnyNumerTelefonu_ZbytDlugi()
        {
            var pacjent = new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa, 02-001, ul. Testowa 5/3",
                Telefon = "1234567890",
                Email = "jan.kowalski@test.pl"
            };
            var kontekstWalidacji = new ValidationContext(pacjent);
            var wyniki = new List<ValidationResult>();

            bool czyPoprawne = Validator.TryValidateObject(pacjent, kontekstWalidacji, wyniki, true);

            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, v => v.ErrorMessage != null && v.ErrorMessage.Contains("9 cyfr"));
        }
    }
}