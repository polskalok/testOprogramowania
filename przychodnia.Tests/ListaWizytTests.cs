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
    public class ListaWizytTests
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

            context.Pacjenci.Add(new Pacjent { ID = 1, Imie = "Pacjent", Nazwisko = "Jeden", Pesel = "11111111111", Adres = "Warszawa", Telefon = "123456789", Email = "a@test.pl" });
            context.Gabinety.Add(new Gabinet { ID = 101, Numer = "101" });

            context.Wizyty.Add(new Wizyta { ID = 1, PacjentID = 1, LekarzID = 2, GabinetID = 101, DataRozpoczecia = DateTime.Now.AddHours(1), Status = "Zarejestrowana" });
            context.Wizyty.Add(new Wizyta { ID = 2, PacjentID = 1, LekarzID = 3, GabinetID = 101, DataRozpoczecia = DateTime.Now.AddHours(2), Status = "Zarejestrowana" });

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
        public void TC_23_01_PrzegladWizyt_Recepcjonista()
        {
            using var context = GetContextWithData("Db_TC_23_01");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.ListaWizyt(null, null, null, null, null) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
            Assert.False((bool)controller.ViewBag.CzyLekarz);
        }

        [Fact]
        public void TC_23_02_PrzegladWizyt_Lekarz()
        {
            using var context = GetContextWithData("Db_TC_23_02");
            var controller = UtworzKontrolerZCiastkiem(context, 2);

            var result = controller.ListaWizyt(null, null, null, null, null) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal(2, model[0].LekarzID);
            Assert.True((bool)controller.ViewBag.CzyLekarz);
        }

        [Fact]
        public void TC_23_03_PrzegladWizyt_BrakWynikow()
        {
            using var context = GetContextWithData("Db_TC_23_03");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var dataOd = new DateTime(2000, 1, 1);
            var dataDo = new DateTime(2000, 1, 2);

            var result = controller.ListaWizyt(null, null, null, dataOd, dataDo) as ViewResult;
            var model = result?.Model as List<Wizyta>;

            Assert.NotNull(model);
            Assert.Empty(model);
            Assert.Equal("Nie znaleziono wizyt spełniających kryteria.", controller.ViewBag.Komunikat);
        }
    }
}