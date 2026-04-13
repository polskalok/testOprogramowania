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
        public void TC_3_DodanieUzytkownika_DuplikatLoginu_ZwracaBlad()
        {
            
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "admin123", Haslo = "Haslo1!", Email = "stary@test.pl", Pesel = "11111111111" });
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var nowyUzytkownik = new Uzytkownik { Login = "admin123", Haslo = "Haslo2!", Email = "nowy@test.pl", Pesel = "22222222222", Plec = "M" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

            
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Login"));
            Assert.Equal("Login już istnieje", controller.ModelState["Login"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_4_DodanieUzytkownika_DuplikatPesel_ZwracaBlad()
        {
           
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "staryLogin", Haslo = "Haslo1!", Email = "stary@test.pl", Pesel = "85010112345" });
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var nowyUzytkownik = new Uzytkownik { Login = "nowyLogin", Haslo = "Haslo2!", Email = "nowy@test.pl", Pesel = "85010112345", Plec = "M" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

            
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL już istnieje w systemie", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_5_DodanieUzytkownika_DuplikatEmail_ZwracaBlad()
        {
            
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "staryLogin", Haslo = "Haslo1!", Email = "jan.kowalski@test.pl", Pesel = "11111111111" });
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var nowyUzytkownik = new Uzytkownik { Login = "nowyLogin", Haslo = "Haslo2!", Email = "jan.kowalski@test.pl", Pesel = "22222222222", Plec = "M" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

            
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Email"));
            Assert.Equal("E-mail już istnieje w systemie", controller.ModelState["Email"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_6_DodanieUzytkownika_NiepoprawnyPeselData_ZwracaBlad()
        {
            
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            
            var nowyUzytkownik = new Uzytkownik { Login = "test1", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85013212345", Plec = "M" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

            
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna data", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_7_DodanieUzytkownika_NiepoprawnyPeselPlec_ZwracaBlad()
        {
            
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            
            var nowyUzytkownik = new Uzytkownik { Login = "test2", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85010112346", Plec = "M" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

           
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna płeć", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_8_DodanieUzytkownika_NiepoprawnyPeselCyfraKontrolna_ZwracaBlad()
        {
            
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            
            var nowyUzytkownik = new Uzytkownik { Login = "test3", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85010112344", Plec = "K" };

            
            controller.DodajUzytkownik(nowyUzytkownik);

            
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna cyfra kontrolna", controller.ModelState["Pesel"]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_14_ZapomnienieUzytkownika_AnonimizujeDaneZgodnieZRODO()
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

            
            controller.Zapomnij(uzytkownik.ID);

            
            var zapomniany = db.Uzytkownicy.First();
            Assert.False(zapomniany.CzyAktywny); 
            Assert.Equal(0, zapomniany.Permisje); 
            Assert.NotEqual("jan.kowalski", zapomniany.Login);
            Assert.NotEqual("Jan", zapomniany.Imie); 
            Assert.NotEqual("jan@test.pl", zapomniany.Email); 
        }
        [Fact]
        public void TC_16_WyswietlanieListy_PokazujeTylkoAktywnych()
        {
            
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "aktywny123", CzyAktywny = true, Nazwisko = "Kowalski", Email = "a@test.pl", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "zapomniany123", CzyAktywny = false, Nazwisko = "Zamazany", Email = "z@test.pl", Pesel = "22222222222" });
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
        public void TC_17_Wyszukiwanie_PoLoginie()
        {
           
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "jan.kowalski", CzyAktywny = true, Nazwisko = "Kowalski", Email = "j@test.pl", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "inny.login", CzyAktywny = true, Nazwisko = "Nowak", Email = "i@test.pl", Pesel = "22222222222" });
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var result = controller.AdminPanel(searchString: "jan.kowalski", showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            
            Assert.NotNull(model);
            Assert.Single(model); 
            Assert.Equal("jan.kowalski", model[0].Login);
        }
        [Fact]
        public void TC_18_Wyszukiwanie_BrakWynikow_ZwracaPustaListe()
        {
            
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "jan", CzyAktywny = true, Nazwisko = "Kowalski", Pesel = "11111111111" });
            db.SaveChanges();
            var controller = new AccountController(db);

            
            var result = controller.AdminPanel(searchString: "nieistniejacyuserxyz123", showForgotten: false) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            
            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void TC_19_Wyswietlanie_ListaZapomnianych_PokazujeTylkoZanonimizowanych()
        {
            
            var db = PrzygotujSztucznaBaze();
            db.Uzytkownicy.Add(new Uzytkownik { Login = "aktywny", CzyAktywny = true, Nazwisko = "Kowalski", Pesel = "11111111111" });
            db.Uzytkownicy.Add(new Uzytkownik { Login = "zapomniany", CzyAktywny = false, Nazwisko = "Zamazany", Pesel = "22222222222" });
            db.SaveChanges();
            var controller = new AccountController(db);

            
            var result = controller.AdminPanel(searchString: null, showForgotten: true) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;

            
            Assert.NotNull(model);
            Assert.Single(model);
            Assert.False(model[0].CzyAktywny);
        }

        [Fact]
        public void TC_20_PodgladDanych_NiedostepnyDlaUzytkownikowZapomnianych()
        {
            
            var db = PrzygotujSztucznaBaze();
            var zapomniany = new Uzytkownik { Login = "zamazany", CzyAktywny = false, Nazwisko = "Anonim", Pesel = "00000000000" };
            db.Uzytkownicy.Add(zapomniany);
            db.SaveChanges();
            var controller = new AccountController(db);

            
            var result = controller.Podglad(zapomniany.ID);

            
            Assert.IsNotType<ViewResult>(result);
        }
    }
}