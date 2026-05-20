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
    public class ListaPacjentowTests
    {
        [Fact]
        public void TC_18_01_PrzegladListyPacjentow_ListaNiepusta()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_18_01")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Pacjenci.Add(new Pacjent
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "123456789",
                Email = "jan@test.pl"
            });
            context.SaveChanges();

            var controller = new AccountController(context);

            var result = controller.PracownikLista(null) as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Single(model);
        }

        [Fact]
        public void TC_18_02_PrzegladListyPacjentow_ListaPusta()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_18_02")
                .Options;

            using var context = new ApplicationDbContext(options);
            var controller = new AccountController(context);

            var result = controller.PracownikLista(null) as ViewResult;
            var model = result?.Model as List<Pacjent>;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Empty(model);
        }
    }
}