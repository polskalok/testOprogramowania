using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using Xunit;

namespace przychodnia.Tests.Scenariusze
{
    public class KontrolerUzytkownikaTests
    {

        private ApplicationDbContext PrzygotujSztucznaBaze()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public void TC_3_DodanieUzytkownika_DuplikatLoginu()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "admin123", Haslo = "MocneHaslo123!", Email = "stary@test.pl", Pesel = "11111111111" });
            db.SaveChanges();

            var controller = new AccountController(db);
            
            var nowyUzytkownik = new Uzytkownik { Login = "admin123", Haslo = "MocneHaslo123!", Email = "nowy@test.pl", Pesel = "22222222222", Plec = "M" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Login"));
            Assert.Equal("Login już istnieje", controller.ModelState["Login"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_4_DodanieUzytkownika_DuplikatPESEL()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "staryLogin", Haslo = "MocneHaslo123!", Email = "stary@test.pl", Pesel = "85010112345" });
            db.SaveChanges();

            var controller = new AccountController(db);
            var nowyUzytkownik = new Uzytkownik { Login = "nowyLogin", Haslo = "MocneHaslo123!", Email = "nowy@test.pl", Pesel = "85010112345", Plec = "M" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL już istnieje w systemie", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_5_DodanieUzytkownika_DuplikatEmail()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "staryLogin", Haslo = "MocneHaslo123!", Email = "jan.kowalski@test.pl", Pesel = "11111111111" });
            db.SaveChanges();

            var controller = new AccountController(db);
            var nowyUzytkownik = new Uzytkownik { Login = "nowyLogin", Haslo = "MocneHaslo123!", Email = "jan.kowalski@test.pl", Pesel = "22222222222", Plec = "M" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Email"));
            Assert.Equal("E-mail już istnieje w systemie", controller.ModelState["Email"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_6_DodanieUzytkownika_NiepoprawnyPeselData()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            var nowyUzytkownik = new Uzytkownik { Login = "test1", Haslo = "MocneHaslo123!", Email = "test@test.pl", Pesel = "85013212345", Plec = "M" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna data", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_7_DodanieUzytkownika_NiepoprawnyPeselPlec()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            var nowyUzytkownik = new Uzytkownik { Login = "test2", Haslo = "MocneHaslo123!", Email = "test@test.pl", Pesel = "85010112346", Plec = "M" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            // Używamy długiej kreski, tak jak wpisali to devowie w kodzie
            Assert.Equal("PESEL nieprawidłowy – niepoprawna płeć", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_8_DodanieUzytkownika_NiepoprawnyPeselCyfraKontrolna()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            var nowyUzytkownik = new Uzytkownik { Login = "test3", Haslo = "MocneHaslo123!", Email = "test@test.pl", Pesel = "85010112344", Plec = "K" };

            controller.DodajUzytkownik(nowyUzytkownik);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            // UWAGA: Ten test BĘDZIE CZERWONY, dopóki chłopaki nie naprawią u siebie tego "Błąd! Wyliczona...", o czym już im pisałeś!
            Assert.Equal("PESEL nieprawidłowy - niepoprawna cyfra kontrolna", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_15_EdycjaUzytkownika_PoprawnyScenariusz()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "MocneHaslo123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan.kowalski@test.pl",
                Pesel = "85010112345"
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);

            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            uzytkownik.Imie = "Janusz";
            uzytkownik.Email = "janusz.kowalski@test.pl";

            controller.Edytuj(uzytkownik);

            var zaktualizowany = db.Uzytkownicy.First();
            Assert.Equal("Janusz", zaktualizowany.Imie);
            Assert.Equal("janusz.kowalski@test.pl", zaktualizowany.Email);
        }

        [Fact]
        public void TC_16_EdycjaUzytkownika_NaruszenieWalidacji()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan.kowalski@test.pl",
                Pesel = "85010112345"
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);

            var zepsuteDane = new Uzytkownik
            {
                ID = uzytkownik.ID,
                Login = "jan.kowalski",
                Imie = "Janusz",
                Nazwisko = "Kowalski",
                Email = "niepoprawny@email",
                Pesel = "85010112345"
            };

            controller.ModelState.AddModelError("Email", "Walidacja danych niepoprawna");

            var result = controller.Edytuj(zepsuteDane);

            Assert.False(controller.ModelState.IsValid);

            var sprawdzWBazie = db.Uzytkownicy.First();
            Assert.Equal("jan.kowalski@test.pl", sprawdzWBazie.Email);
            Assert.Equal("Jan", sprawdzWBazie.Imie);
        }

        [Fact]
        public void TC_17_ZapomnienieUzytkownika_PoprawnyScenariusz()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "Tajne123!",
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan@test.pl",
                Pesel = "85010112345",
                CzyAktywny = true,
                Permisje = 1
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);

            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            controller.Zapomnij(uzytkownik.ID);

            var zapomniany = db.Uzytkownicy.First();
            Assert.False(zapomniany.CzyAktywny);
            Assert.Equal(0, zapomniany.Permisje);
            Assert.NotEqual("jan.kowalski", zapomniany.Login);
            Assert.NotEqual("Jan", zapomniany.Imie);
            Assert.NotEqual("jan@test.pl", zapomniany.Email);
        }

        [Fact]
        public void TC_20_WyswietlanieListy_TylkoAktywniUzytkownicy()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "aktywny123", CzyAktywny = true, Imie = "Jan", Nazwisko = "Kowalski", Email = "a@test.pl", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "zapomniany123", CzyAktywny = false, Imie = "Zamazany", Nazwisko = "Zamazany", Email = "z@test.pl", Pesel = "22222222222" });
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: null, showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.True(model[0].CzyAktywny);
            Assert.Equal("aktywny123", model[0].Login);
        }

        [Fact]
        public void TC_21_WyswietlanieListy_BrakUzytkownikow()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: null, showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void TC_22_WyszukiwanieUzytkownikow_PoLoginie()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "jan.kowalski", CzyAktywny = true, Imie = "Jan", Nazwisko = "Kowalski", Email = "j@test.pl", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "inny.login", CzyAktywny = true, Imie = "Inny", Nazwisko = "Nowak", Email = "i@test.pl", Pesel = "22222222222" });
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: "jan.kowalski", showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("jan.kowalski", model[0].Login);
        }

        [Fact]
        public void TC_23_WyszukiwanieUzytkownikow_BrakWynikow()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "jan.kowalski", CzyAktywny = true, Imie = "Jan", Nazwisko = "Kowalski", Email = "j@test.pl", Pesel = "11111111111" });
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: "nieistniejacyuserxyz123", showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void TC_24_WyswietlanieListy_UzytkownicyZapomniani()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "aktywny", CzyAktywny = true, Imie = "Jan", Nazwisko = "Kowalski", Email = "a@test.pl", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "zapomniany", CzyAktywny = false, Imie = "Zamazane", Nazwisko = "Zamazane", Email = "z@test.pl", Pesel = "22222222222" });
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: null, showForgotten: true) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Single(model);
            Assert.False(model[0].CzyAktywny);
        }

        [Fact]
        public void TC_25_WyszukiwanieUzytkownikowZapomnianych_BrakWynikow()
        {
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "zapomniany", CzyAktywny = false, Imie = "Zamazane", Nazwisko = "Zamazane", Email = "z@test.pl", Pesel = "22222222222" });
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.AdminPanel(searchString: "nieistniejacy", showForgotten: true) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void TC_26_PodgladDanych_WyswietlaPelneDaneAktywnegoUzytkownika()
        {
            var db = PrzygotujSztucznaBaze();
            var aktywny = new Uzytkownik { Login = "jan.kowalski", CzyAktywny = true, Imie = "Jan", Nazwisko = "Kowalski", Email = "jan@test.pl", Pesel = "12345678901" };
            db.Uzytkownicy.Add(aktywny);
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.Podglad(aktywny.ID) as ViewResult;
            var model = result?.Model as Uzytkownik;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Equal("jan.kowalski", model.Login);
            Assert.Equal("Jan", model.Imie);
            Assert.Equal("Kowalski", model.Nazwisko);
        }

        [Fact]
        public void TC_26b_PodgladDanych_NiedostepnyDlaUzytkownikowZapomnianych()
        {
            var db = PrzygotujSztucznaBaze();
            var zapomniany = new Uzytkownik { Login = "zamazany", CzyAktywny = false, Imie = "Zamazany", Nazwisko = "Zamazany", Email = "z@test.pl", Pesel = "00000000000" };
            db.Uzytkownicy.Add(zapomniany);
            db.SaveChanges();

            var controller = new AccountController(db);

            var result = controller.Podglad(zapomniany.ID);

            Assert.IsNotType<ViewResult>(result);
        }
    }
}