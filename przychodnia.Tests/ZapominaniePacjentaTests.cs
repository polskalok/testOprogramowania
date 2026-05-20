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
    public class ZapominaniePacjentaTests
    {
        [Fact]
        public void TC_D3_ZapomnieniePacjenta()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Db_TC_D3")
                .Options;

            using var context = new ApplicationDbContext(options);

            context.Pacjenci.Add(new Pacjent
            {
                ID = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Pesel = "85010112345",
                Adres = "Warszawa",
                Telefon = "123456789",
                Email = "jan@test.pl"
            });
            context.SaveChanges();

            var controller = new AccountController(context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
            };

            var result = controller.ZapomnijPacjenta(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PracownikLista", redirectResult.ActionName);

            var deleted = context.Pacjenci.FirstOrDefault(p => p.ID == 1);
            Assert.Null(deleted);
        }
    }
}