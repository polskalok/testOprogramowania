using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Controllers;
using przychodnia.Data;
using przychodnia.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace przychodnia.Tests
{
    public class UprawnieniaTests
    {
        private ApplicationDbContext PrzygotujSztucznaBaze()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }


        [Fact]
        public void TC_1_Modul2_PrzegladListyWszystkichUprawnien_WyswietlaKompletnaListe()
        {
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);


            var result = controller.ListaUprawnien() as ViewResult;


            var model = result?.Model as IEnumerable<dynamic>;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Equal(3, model.Count());
        }

        [Fact]
        public void TC_2_Modul2_NadanieUprawnienUzytkownikowi_ZapisujeZmianyWBazie()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "Test1!",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan@test.pl",
                Pesel = "12345678901",
                Permisje = 0
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            int[] przypisaneRole = new int[] { 1, 2 };


            var result = controller.Uprawnienia(uzytkownik.ID, przypisaneRole) as RedirectToActionResult;


            var zaktualizowany = db.Uzytkownicy.First();
            Assert.Equal(3, zaktualizowany.Permisje);


            Assert.NotNull(result);
            Assert.Equal("Podglad", result.ActionName);
        }

        [Fact]
        public void TC_3_Modul2_NadanieUprawnien_BrakWyboru()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "jan.kowalski",
                Haslo = "Test1!",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan@test.pl",
                Pesel = "12345678901",
                Permisje = 4
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            int[] pusteRole = new int[0];
            var result = controller.Uprawnienia(uzytkownik.ID, pusteRole) as ViewResult;


            Assert.NotNull(result);
            Assert.Equal("nie zaznaczono żadnych uprawnień", controller.ViewBag.Error);
        }

        [Fact]
        public void TC_4_Modul2_PrzegladUzytkownikowPDanymUprawnieniem()
        {
            var db = PrzygotujSztucznaBaze();


            db.Uzytkownicy.Add(new Uzytkownik { Login = "admin1", CzyAktywny = true, Imie = "A", Nazwisko = "B", Email = "a@test.pl", Pesel = "11111111111", Permisje = 1 }); // Admin
            db.Uzytkownicy.Add(new Uzytkownik { Login = "pracownik1", CzyAktywny = true, Imie = "C", Nazwisko = "D", Email = "b@test.pl", Pesel = "22222222222", Permisje = 2 }); // Pracownik
            db.Uzytkownicy.Add(new Uzytkownik { Login = "super_admin", CzyAktywny = true, Imie = "E", Nazwisko = "F", Email = "c@test.pl", Pesel = "33333333333", Permisje = 3 }); // Admin (1) + Pracownik (2) = 3
            db.Uzytkownicy.Add(new Uzytkownik { Login = "nieaktywny_admin", CzyAktywny = false, Imie = "G", Nazwisko = "H", Email = "d@test.pl", Pesel = "44444444444", Permisje = 1 }); // Usunięty admin
            db.SaveChanges();

            var controller = new AccountController(db);


            var result = controller.UzytkownicyZUprawnieniem(1) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;


            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
            Assert.Contains(model, u => u.Login == "admin1");
            Assert.Contains(model, u => u.Login == "super_admin");
            Assert.DoesNotContain(model, u => u.Login == "pracownik1");
            Assert.DoesNotContain(model, u => u.Login == "nieaktywny_admin");
        }

        [Fact]
        public void TC_5_Modul2_PrzegladUzytkownikowZUprawnieniem_BrakWynikow()
        {
            var db = PrzygotujSztucznaBaze();


            db.Uzytkownicy.Add(new Uzytkownik { Login = "admin_only", CzyAktywny = true, Imie = "A", Nazwisko = "B", Email = "a@test.pl", Pesel = "11111111111", Permisje = 1 });
            db.SaveChanges();

            var controller = new AccountController(db);


            var result = controller.UzytkownicyZUprawnieniem(2) as ViewResult;
            var model = result?.Model as List<Uzytkownik>;


            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void TC_6_Modul2_OdebranieUprawnienUzytkownikowi()
        {
            var db = PrzygotujSztucznaBaze();
            var uzytkownik = new Uzytkownik
            {
                Login = "wielo_rolowy",
                Haslo = "Test1!",
                CzyAktywny = true,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = "jan@test.pl",
                Pesel = "12345678901",
                Permisje = 3
            };
            db.Uzytkownicy.Add(uzytkownik);
            db.SaveChanges();

            var controller = new AccountController(db);


            int[] noweRole = new int[] { 1 };
            var result = controller.Uprawnienia(uzytkownik.ID, noweRole) as RedirectToActionResult;


            var zaktualizowany = db.Uzytkownicy.First();
            Assert.Equal(1, zaktualizowany.Permisje);
        }

        [Fact]
        public void TC_7_Modul2_NadanieUprawnienZapomnianemuUzytkownikowi()
        {
            var db = PrzygotujSztucznaBaze();
            var zapomniany = new Uzytkownik
            {
                Login = "zamazany_login",
                CzyAktywny = false,
                Imie = "Anonim",
                Nazwisko = "Uzytkownik",
                Email = "anonim@test.pl",
                Pesel = "00000000000",
                Permisje = 0
            };
            db.Uzytkownicy.Add(zapomniany);
            db.SaveChanges();

            var controller = new AccountController(db);


            int[] noweRole = new int[] { 1 };
            controller.Uprawnienia(zapomniany.ID, noweRole);


            var stanPoAkcji = db.Uzytkownicy.First();


            Assert.Equal(0, stanPoAkcji.Permisje);
        }
    }
}