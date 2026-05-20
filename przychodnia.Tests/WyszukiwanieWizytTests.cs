using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using Xunit;

namespace przychodnia.Tests.Scenariusze
{
    public class WyszukiwanieWizytTests
    {
        private ApplicationDbContext GetContextWithData(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new ApplicationDbContext(options);

            context.Uzytkownicy.Add(new Uzytkownik { ID = 1, Imie = "Recepcja", Nazwisko = "Testowa", Permisje = 2, CzyAktywny = true, Specjalizacja = "" });
            context.Uzytkownicy.Add(new Uzytkownik { ID = 2, Imie = "Anna", Nazwisko = "Nowak", Permisje = 2, CzyAktywny = true, Specjalizacja = "Kardiolog" });
            context.Uzytkownicy.Add(new Uzytkownik { ID = 3, Imie = "Jan", Nazwisko = "Kowal", Permisje = 2, CzyAktywny = true, Specjalizacja = "Okulista" });

            context.Pacjenci.Add(new Pacjent { ID = 1, Imie = "Jan", Nazwisko = "Kowalski", Pesel = "85010112345", Adres = "Warszawa", Telefon = "123456789", Email = "a@test.pl" });
            context.Gabinety.Add(new Gabinet { ID = 101, Numer = "101" });

            context.Wizyty.Add(new Wizyta { ID = 1, PacjentID = 1, LekarzID = 2, GabinetID = 101, DataRozpoczecia = new DateTime(2026, 5, 10, 10, 0, 0), Status = "Zarejestrowana" });
            context.Wizyty.Add(new Wizyta { ID = 2, PacjentID = 1, LekarzID = 3, GabinetID = 101, DataRozpoczecia = new DateTime(2026, 5, 12, 12, 0, 0), Status = "Zarejestrowana" });

            context.SaveChanges();
            return context;
        }

        private WizytaController UtworzKontrolerZCiastkiem(ApplicationDbContext context, int userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = $"AuthUserId={userId}";
            return new WizytaController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public void TC_24_01_WyszukiwanieWizyt_Recepcjonista_ImieNazwisko()
        {
            using var context = GetContextWithData("Db_TC_24_01");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.ListaWizyt("Jan Kowalski", 2, "Kardiolog", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal(2, model[0].LekarzID);
        }

        [Fact]
        public void TC_24_02_WyszukiwanieWizyt_Lekarz_ImieNazwisko()
        {
            using var context = GetContextWithData("Db_TC_24_02");
            var controller = UtworzKontrolerZCiastkiem(context, 2);

            var result = controller.ListaWizyt("Jan Kowalski", null, null, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal(2, model[0].LekarzID);
        }

        [Fact]
        public void TC_24_03_WyszukiwanieWizyt_Recepcjonista_Pesel()
        {
            using var context = GetContextWithData("Db_TC_24_03");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.ListaWizyt("85010112345", 2, "Kardiolog", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal(1, model[0].PacjentID);
        }

        [Fact]
        public void TC_24_04_WyszukiwanieWizyt_Lekarz_Pesel()
        {
            using var context = GetContextWithData("Db_TC_24_04");
            var controller = UtworzKontrolerZCiastkiem(context, 2);

            var result = controller.ListaWizyt("85010112345", null, null, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal(2, model[0].LekarzID);
        }

        [Fact]
        public void TC_24_05_WyszukiwanieWizyt_BrakKryteriow()
        {
            using var context = GetContextWithData("Db_TC_24_05");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.ListaWizyt(string.Empty, null, string.Empty, null, null) as ViewResult;

            Assert.Equal("Wypełnij co najmniej jedno kryterium", controller.ViewBag.Komunikat);
        }

        [Fact]
        public void TC_24_06_WyszukiwanieWizyt_BrakWynikow()
        {
            using var context = GetContextWithData("Db_TC_24_06");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.ListaWizyt("XYZ123", null, null, null, null) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Empty(model);
            Assert.Equal("Nie znaleziono wizyt spełniających kryteria.", controller.ViewBag.Komunikat);
        }
    }
}