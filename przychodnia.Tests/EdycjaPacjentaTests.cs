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
    public class EdycjaPacjentaTests
    {
        private ApplicationDbContext GetContextWithData(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent { ID = 1, Imie = "Jan", Nazwisko = "Kowalski", Pesel = "85010112345", Adres = "Warszawa", Telefon = "123456789", Email = "jan@test.pl" });
            context.Pacjenci.Add(new Pacjent { ID = 2, Imie = "Anna", Nazwisko = "Nowak", Pesel = "90020211111", Adres = "Kraków", Telefon = "987654321", Email = "anna@test.pl" });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public void TC_20_01_PodgladDanychPacjenta()
        {
            using var context = GetContextWithData("Db_TC_20_01");
            var controller = new AccountController(context);

            var result = controller.PracownikPodglad(1) as ViewResult;
            var model = result?.Model as Pacjent;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Equal("Jan", model.Imie);
            Assert.Equal("85010112345", model.Pesel);
        }

        [Fact]
        public void TC_21_01_EdycjaDanychPacjenta_SciezkaGlowna()
        {
            using var context = GetContextWithData("Db_TC_21_01");
            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var edytowanyPacjent = new Pacjent
            {
                ID = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "111222333",
                Email = "nowy.jan@test.pl"
            };

            var result = controller.EdytujPacjenta(edytowanyPacjent);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("PracownikPodglad", redirectResult.ActionName);

            var zaktualizowany = context.Pacjenci.First(p => p.ID == 1);
            Assert.Equal("111222333", zaktualizowany.Telefon);
            Assert.Equal("nowy.jan@test.pl", zaktualizowany.Email);
        }

        [Fact]
        public void TC_21_02_EdycjaDanychPacjenta_DuplikatPesel()
        {
            using var context = GetContextWithData("Db_TC_21_02");
            var controller = new AccountController(context);

            var edytowanyPacjent = new Pacjent
            {
                ID = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "90020211111",
                Adres = "Warszawa",
                Telefon = "123456789",
                Email = "jan@test.pl"
            };

            var result = controller.EdytujPacjenta(edytowanyPacjent) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
        }

        [Fact]
        public void TC_21_03_EdycjaDanychPacjenta_DuplikatEmail()
        {
            using var context = GetContextWithData("Db_TC_21_03");
            var controller = new AccountController(context);

            var edytowanyPacjent = new Pacjent
            {
                ID = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "123456789",
                Email = "anna@test.pl"
            };

            var result = controller.EdytujPacjenta(edytowanyPacjent) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Email"));
        }
    }
}