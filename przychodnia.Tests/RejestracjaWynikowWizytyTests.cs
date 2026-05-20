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
    public class RejestracjaWynikowWizytyTests
    {
        private ApplicationDbContext GetContextWithData(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new ApplicationDbContext(options);

            context.Uzytkownicy.Add(new Uzytkownik { ID = 1, Imie = "Anna", Nazwisko = "Nowak", Permisje = 2, CzyAktywny = true, Specjalizacja = "Kardiolog" });
            context.Pacjenci.Add(new Pacjent { ID = 1, Imie = "Jan", Nazwisko = "Kowalski", Pesel = "85010112345", Adres = "Warszawa", Telefon = "123456789", Email = "a@test.pl" });
            context.Gabinety.Add(new Gabinet { ID = 101, Numer = "101" });

            context.Wizyty.Add(new Wizyta { ID = 1, PacjentID = 1, LekarzID = 1, GabinetID = 101, DataRozpoczecia = DateTime.Now.AddDays(-1), Status = "Zarejestrowana" });
            context.Wizyty.Add(new Wizyta { ID = 2, PacjentID = 1, LekarzID = 1, GabinetID = 101, DataRozpoczecia = DateTime.Now.AddDays(5), Status = "Zarejestrowana" });
            context.Wizyty.Add(new Wizyta { ID = 3, PacjentID = 1, LekarzID = 1, GabinetID = 101, DataRozpoczecia = DateTime.Now.AddDays(-2), Status = "Zrealizowana" });

            context.SaveChanges();
            return context;
        }

        private WizytaController UtworzKontrolerZCiastkiem(ApplicationDbContext context, int userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cookie"] = $"AuthUserId={userId}";
            return new WizytaController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
            };
        }

        [Fact]
        public void TC_25_01_RejestracjaWynikow_SciezkaGlowna()
        {
            using var context = GetContextWithData("Db_TC_25_01");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.UzupelnijWyniki(1, "Ból w klatce piersiowej", "Kontrola za 2 tyg.", "Aspiryna 75mg");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListaWizyt", redirectResult.ActionName);

            var zaktualizowanaWizyta = context.Wizyty.First(w => w.ID == 1);
            Assert.Equal("Zrealizowana", zaktualizowanaWizyta.Status);
            Assert.Equal("Ból w klatce piersiowej", zaktualizowanaWizyta.OpisDoleglywosci);
            Assert.Equal("Kontrola za 2 tyg.", zaktualizowanaWizyta.Zalecenia);
            Assert.Equal("Aspiryna 75mg", zaktualizowanaWizyta.PrzepisaneLeki);
        }

        [Fact]
        public void TC_25_02_RejestracjaWynikow_WizytaPrzyszla()
        {
            using var context = GetContextWithData("Db_TC_25_02");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.UzupelnijWyniki(2);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListaWizyt", redirectResult.ActionName);
            Assert.Equal("Wizyta jeszcze się nie odbyła.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void TC_25_03_RejestracjaWynikow_WizytaJuzZrealizowana()
        {
            using var context = GetContextWithData("Db_TC_25_03");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.UzupelnijWyniki(3) as ViewResult;

            Assert.NotNull(result);
            Assert.True((bool)controller.ViewBag.TrybPodgladu);
        }

        [Fact]
        public void TC_25_04_RejestracjaWynikow_BrakObowiazkowychPol()
        {
            using var context = GetContextWithData("Db_TC_25_04");
            var controller = UtworzKontrolerZCiastkiem(context, 1);

            var result = controller.UzupelnijWyniki(1, string.Empty, string.Empty, string.Empty) as ViewResult;

            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("nie mogą pozostać puste"));
            Assert.False((bool)controller.ViewBag.TrybPodgladu);
        }
    }
}