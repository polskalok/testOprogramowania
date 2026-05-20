using System;
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
    public class RejestracjaWizytyTests
    {
        private ApplicationDbContext GetContextWithData(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent { ID = 1, Imie = "Jan", Nazwisko = "Kowalski", Pesel = "85010112345", Adres = "Warszawa", Telefon = "123456789", Email = "jan@test.pl" });
            context.Uzytkownicy.Add(new Uzytkownik { ID = 1, Imie = "Anna", Nazwisko = "Nowak", Permisje = 2, CzyAktywny = true, Specjalizacja = "Kardiolog" });
            context.Gabinety.Add(new Gabinet { ID = 101, Numer = "101" });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public void TC_22_01_RejestracjaWizyty_SciezkaGlowna()
        {
            using var context = GetContextWithData("Db_TC_22_01");
            var controller = new WizytaController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var wizyta = new Wizyta
            {
                PacjentID = 1,
                LekarzID = 1,
                GabinetID = 101,
                DataRozpoczecia = new DateTime(2026, 5, 15, 10, 30, 0)
            };

            var result = controller.Rejestruj(wizyta);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Rejestruj", redirectResult.ActionName);

            var zapisanaWizyta = context.Wizyty.FirstOrDefault();
            Assert.NotNull(zapisanaWizyta);
            Assert.Equal("Zarejestrowana", zapisanaWizyta.Status);
            Assert.Equal(new DateTime(2026, 5, 15, 11, 0, 0), zapisanaWizyta.DataZakonczenia);
        }

        [Fact]
        public void TC_22_RejestracjaWizyty_KonfliktTerminow_ZajetyGabinet()
        {
            using var context = GetContextWithData("Db_TC_22_Konflikt");
            var controller = new WizytaController(context);

            context.Wizyty.Add(new Wizyta
            {
                PacjentID = 1,
                LekarzID = 1,
                GabinetID = 101,
                DataRozpoczecia = new DateTime(2026, 5, 15, 10, 30, 0),
                DataZakonczenia = new DateTime(2026, 5, 15, 11, 0, 0),
                Status = "Zarejestrowana"
            });
            context.SaveChanges();

            var nowaWizyta = new Wizyta
            {
                PacjentID = 1,
                LekarzID = 1,
                GabinetID = 101,
                DataRozpoczecia = new DateTime(2026, 5, 15, 10, 45, 0)
            };

            var result = controller.Rejestruj(nowaWizyta) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("DataRozpoczecia"));
        }

        [Fact]
        public void TC_22_05_RejestracjaWizyty_BrakWolnychTerminow()
        {
            using var context = GetContextWithData("Db_TC_22_05");
            var controller = new WizytaController(context);

            context.Wizyty.Add(new Wizyta
            {
                PacjentID = 1,
                LekarzID = 1,
                GabinetID = 101,
                DataRozpoczecia = new DateTime(2026, 5, 20, 10, 30, 0),
                DataZakonczenia = new DateTime(2026, 5, 20, 11, 0, 0),
                Status = "Zarejestrowana"
            });
            context.SaveChanges();

            var nowaWizyta = new Wizyta
            {
                PacjentID = 1,
                LekarzID = 1,
                GabinetID = 101,
                DataRozpoczecia = new DateTime(2026, 5, 20, 10, 30, 0)
            };

            var result = controller.Rejestruj(nowaWizyta) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("DataRozpoczecia"));
        }

        [Fact]
        public void TC_22_06_RejestracjaWizyty_BrakWymaganychPol()
        {
            using var context = GetContextWithData("Db_TC_22_06");
            var controller = new WizytaController(context);

            var nowaWizyta = new Wizyta
            {
                PacjentID = 0,
                LekarzID = 0,
                GabinetID = 0
            };

            var result = controller.Rejestruj(nowaWizyta) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("PacjentID"));
            Assert.True(controller.ModelState.ContainsKey("LekarzID"));
            Assert.True(controller.ModelState.ContainsKey("GabinetID"));
        }
    }
}