using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using Xunit;

namespace przychodnia.Tests.Scenariusze
{
    public class WyszukiwaniePacjentowTests
    {
        private ApplicationDbContext GetContextWithData(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent { Imie = "Jan", Nazwisko = "Kowalski", Pesel = "85010112345", Adres = "Warszawa Testowa 5", Telefon = "123456789", Email = "jan@test.pl" });
            context.Pacjenci.Add(new Pacjent { Imie = "Anna", Nazwisko = "Nowak", Pesel = "90020211111", Adres = "Kraków Długa 10", Telefon = "987654321", Email = "anna@test.pl" });
            context.Pacjenci.Add(new Pacjent { Imie = "Tomasz", Nazwisko = "Kowal", Pesel = "88030322222", Adres = "Poznań Krótka 1", Telefon = "48123456789", Email = "tomasz@test.pl" });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public void TC_19_01_WyszukiwaniePoPesel()
        {
            using var context = GetContextWithData("Db_TC_19_01");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("85010112345") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Jan", model[0].Imie);
        }

        [Fact]
        public void TC_19_02_WyszukiwaniePoNumerzeTelefonu_Czesciowe()
        {
            using var context = GetContextWithData("Db_TC_19_02");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("1234567") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            
            Assert.Equal(2, model.Count);
        }
        

        [Fact]
        public void TC_19_03_WyszukiwaniePoNumerzeTelefonu_CaloscioweZKierunkowym()
        {
            using var context = GetContextWithData("Db_TC_19_03");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("+48123456789") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Tomasz", model[0].Imie);
        }

        [Fact]
        public void TC_19_04_WyszukiwaniePoImieniu()
        {
            using var context = GetContextWithData("Db_TC_19_04");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("Jan") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Kowalski", model[0].Nazwisko);
        }

        [Fact]
        public void TC_19_05_WyszukiwaniePoNazwisku()
        {
            using var context = GetContextWithData("Db_TC_19_05");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("Kowalski") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Jan", model[0].Imie);
        }

        [Fact]
        public void TC_19_06_WyszukiwaniePoAdresie_Miejscowosc()
        {
            using var context = GetContextWithData("Db_TC_19_06");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("Warszawa") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Jan", model[0].Imie);
        }

        [Fact]
        public void TC_19_07_WyszukiwaniePoAdresie_Ulica()
        {
            using var context = GetContextWithData("Db_TC_19_07");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("Testowa") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("Jan", model[0].Imie);
        }

        [Fact]
        public void TC_19_08_WyszukiwaniePelnotekstowe()
        {
            using var context = GetContextWithData("Db_TC_19_08");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("Kowal") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public void TC_19_09_Wyszukiwanie_BrakWynikow()
        {
            using var context = GetContextWithData("Db_TC_19_09");
            var controller = new AccountController(context);

            var result = controller.PracownikLista("XYZ123") as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(model);
            Assert.Empty(model);
        }
    }
}