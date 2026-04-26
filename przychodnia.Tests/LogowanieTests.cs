using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using przychodnia.Services;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace przychodnia.Tests
{
    public class LogowanieTests
    {
        private ApplicationDbContext PrzygotujSztucznaBaze()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }


        [Fact]
        public void TC_01_Modul3_PoprawneLogowanie()
        {
            var db = PrzygotujSztucznaBaze();


            var zaszyfrowaneHaslo = PasswordHasher.HashPassword("MojeTajneHaslo123!");
            var uzytkownik = new Uzytkownik
            {
                Login = "admin_test",
                Haslo = zaszyfrowaneHaslo,
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan@test.pl",
                Pesel = "11111111111",
                Permisje = 1
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };


            var result = controller.Login("admin_test", "MojeTajneHaslo123!") as RedirectToActionResult;


            Assert.NotNull(result);
            Assert.Equal("AdminPanel", result.ActionName);
        }

        [Fact]
        public void TC_02_Modul3_BledneHaslo()
        {
            var db = PrzygotujSztucznaBaze();

            var zaszyfrowaneHaslo = PasswordHasher.HashPassword("PrawdziweHaslo123!");
            var uzytkownik = new Uzytkownik
            {
                Login = "user_test",
                Haslo = zaszyfrowaneHaslo,
                CzyAktywny = true,
                Imie = "Anna",
                Nazwisko = "Nowak",
                Email = "anna@test.pl",
                Pesel = "22222222222",
                Permisje = 4
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            var result = controller.Login("user_test", "ZupelnieZleHaslo999!") as ViewResult;


            Assert.NotNull(result);
            Assert.Equal("Błędne hasło lub login", controller.ViewBag.Error);
        }

        [Fact]
        public void TC_03_Modul3_BlokadaKontaPo3Probach()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "lockout_user",
                Haslo = PasswordHasher.HashPassword("MojeHaslo123!"),
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111"
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            controller.Login("lockout_user", "ZleHaslo1!");
            controller.Login("lockout_user", "ZleHaslo2!");
            controller.Login("lockout_user", "ZleHaslo3!");


            var result = controller.Login("lockout_user", "ZleHaslo4!") as ViewResult;


            Assert.NotNull(result);
            Assert.Contains("Konto zostało zablokowane czasowo", controller.ViewBag.Error?.ToString() ?? "");
        }

        [Fact]
        public void TC_04_Modul3_LogowanieNaZablokowaneKonto()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "blocked_user",
                Haslo = PasswordHasher.HashPassword("PoprawneHaslo123!"),
                CzyAktywny = true,
                Imie = "Anna",
                Nazwisko = "Nowak",
                Email = "a@test.pl",
                Pesel = "22222222222"
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            controller.Login("blocked_user", "ZleHaslo1!");
            controller.Login("blocked_user", "ZleHaslo2!");
            controller.Login("blocked_user", "ZleHaslo3!");


            var result = controller.Login("blocked_user", "PoprawneHaslo123!");


            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains("Konto zostało zablokowane czasowo", controller.ViewBag.Error?.ToString() ?? "");
        }

        [Fact]
        public void TC_05_Modul3_WylogowanieZSystemu()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);


            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };


            var result = controller.Logout() as RedirectToActionResult;


            Assert.NotNull(result);

            Assert.Equal("Login", result.ActionName);
        }

        [Fact]
        public void TC_08_Modul3_WalidacjaKryteriowHasla()
        {

            var uzytkownik = new Uzytkownik
            {
                Login = "nowy_testowy",
                Haslo = "slabe",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111",
                Telefon = "123456789"
            };

            var kontekstWalidacji = new System.ComponentModel.DataAnnotations.ValidationContext(uzytkownik);
            var wyniki = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();

            bool czyPoprawne = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);


            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Haslo"));
        }

        [Fact]
        public void TC_09_Modul3_WalidacjaHasla_BrakZnakuSpecjalnego()
        {

            var uzytkownik = new Uzytkownik
            {
                Login = "test_09",
                Haslo = "WielkaMala1",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111"
            };

            var kontekstWalidacji = new System.ComponentModel.DataAnnotations.ValidationContext(uzytkownik);
            var wyniki = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool czyPoprawne = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);


            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Haslo"));
        }

        [Fact]
        public void TC_10_Modul3_WalidacjaHasla_BrakCyfry()
        {

            var uzytkownik = new Uzytkownik
            {
                Login = "test_10",
                Haslo = "WielkaMala!",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111"
            };

            var kontekstWalidacji = new System.ComponentModel.DataAnnotations.ValidationContext(uzytkownik);
            var wyniki = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool czyPoprawne = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);


            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Haslo"));
        }

        [Fact]
        public void TC_11_Modul3_WalidacjaHasla_BrakWielkichIMalychLiter()
        {

            var uzytkownik = new Uzytkownik
            {
                Login = "test_11",
                Haslo = "samemale1!",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111"
            };

            var kontekstWalidacji = new System.ComponentModel.DataAnnotations.ValidationContext(uzytkownik);
            var wyniki = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool czyPoprawne = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);


            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Haslo"));
        }

        [Fact]
        public void TC_12_Modul3_WalidacjaHasla_NieprawidlowaDlugosc()
        {

            var uzytkownik = new Uzytkownik
            {
                Login = "test_12",
                Haslo = "Aa1!x",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "j@test.pl",
                Pesel = "11111111111"
            };

            var kontekstWalidacji = new System.ComponentModel.DataAnnotations.ValidationContext(uzytkownik);
            var wyniki = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool czyPoprawne = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(uzytkownik, kontekstWalidacji, wyniki, true);


            Assert.False(czyPoprawne);
            Assert.Contains(wyniki, w => w.MemberNames.Contains("Haslo"));
        }

        [Fact]
        public async Task TC_13_Modul3_OdzyskiwanieHasla_PoprawneDane()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "zapominalski",
                Email = "ratunku@test.pl",
                CzyAktywny = true,
                Haslo = "StareHaslo123!"
            };
            db.Uzytkownicy.Add(uzytkownik);
            await db.SaveChangesAsync();

            var controller = new AccountController(db);

            
            var result = await controller.OdzyskajHaslo("zapominalski", "ratunku@test.pl") as RedirectToActionResult;

            
            var zaktualizowany = db.Uzytkownicy.First();
            Assert.True(zaktualizowany.MuszZmieniHaslo); 
            Assert.NotEqual("StareHaslo123!", zaktualizowany.Haslo); 
        }

        [Fact]
        public async Task TC_14_Modul3_OdzyskiwanieHasla_BledneDane()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);

            
            var result = await controller.OdzyskajHaslo("nieznany", "zlymail@test.pl") as ViewResult;

            Assert.NotNull(result);
            Assert.Equal("Podane dane nie pasują do żadnego konta w systemie.", controller.ViewBag.Error);
        }

        [Fact]
        public void TC_16_Modul3_WymuszonaZmianaHaslaPoResecie()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "user_reset",
                Haslo = PasswordHasher.HashPassword("Tymczasowe1!"),
                CzyAktywny = true,
                MuszZmieniHaslo = true 
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            
            var result = controller.Login("user_reset", "Tymczasowe1!") as RedirectToActionResult;

            
            Assert.NotNull(result);
            Assert.Equal("MuszZmieniHaslo", result.ActionName);
        }

        [Fact]
        public void TC_17_Modul3_WeryfikacjaZgodnosciHasel()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik { Login = "test_17", CzyAktywny = true };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var result = controller.ZmieniHaslo(uzytkownik.ID, "NoweHaslo123!", "CalkowicieInneHaslo123!") as ViewResult;

            Assert.NotNull(result);
            Assert.Equal("Upewnij się że hasła są identyczne", controller.ViewBag.Error);
        }

        [Fact]
        public void TC_18_Modul3_WalidacjaHistoriiHasel()
        {
            var db = PrzygotujSztucznaBaze();

            
            var uzytkownik = new Uzytkownik
            {
                Login = "historia_test",
                CzyAktywny = true,
                OstatniaHasla = PasswordHasher.HashPassword("StareHaslo1!")
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);

            
            var result = controller.ZmieniHaslo(uzytkownik.ID, "StareHaslo1!", "StareHaslo1!") as ViewResult;

            
            Assert.NotNull(result);
            Assert.Equal("Nowe hasło musi różnić się od 3 ostatnich używanych haseł.", controller.ViewBag.Error);
        }
    }
}