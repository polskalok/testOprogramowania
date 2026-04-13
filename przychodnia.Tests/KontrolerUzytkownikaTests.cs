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
            // Arrange
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            // Używamy PESELu z błędem w dacie (32 stycznia) - 85013212345
            var nowyUzytkownik = new Uzytkownik { Login = "test1", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85013212345", Plec = "M" };

            // Act
            controller.DodajUzytkownik(nowyUzytkownik);

            // Assert
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna data", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_7_DodanieUzytkownika_NiepoprawnyPeselPlec_ZwracaBlad()
        {
            // Arrange
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            // Używamy PESELu, gdzie cyfra płci (przedostatnia: 4) sugeruje kobietę, a podajemy "M"
            var nowyUzytkownik = new Uzytkownik { Login = "test2", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85010112346", Plec = "M" };

            // Act
            controller.DodajUzytkownik(nowyUzytkownik);

            // Assert
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna płeć", controller.ModelState["Pesel"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_8_DodanieUzytkownika_NiepoprawnyPeselCyfraKontrolna_ZwracaBlad()
        {
            // Arrange
            var db = PrzygotujSztucznaBaze();
            var controller = new AccountController(db);
            // Używamy PESELu ze złą ostatnią cyfrą (zamiast 5 jest 4) i dajemy Płeć "K" żeby system przeszedł dalej
            var nowyUzytkownik = new Uzytkownik { Login = "test3", Haslo = "Haslo1!", Email = "test@test.pl", Pesel = "85010112344", Plec = "K" };

            // Act
            controller.DodajUzytkownik(nowyUzytkownik);

            // Assert
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Pesel"));
            Assert.Equal("PESEL nieprawidłowy – niepoprawna cyfra kontrolna", controller.ModelState["Pesel"]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public void TC_14_ZapomnienieUzytkownika_AnonimizujeDaneZgodnieZRODO()
        {
            // Arrange - Tworzymy normalnego użytkownika z uprawnieniami
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

            // Act - Uruchamiamy procedurę RODO
            controller.Zapomnij(uzytkownik.ID);

            // Assert - Sprawdzamy co zostało w bazie
            var zapomniany = db.Uzytkownicy.First();
            Assert.False(zapomniany.CzyAktywny); // Konto musi być nieaktywne
            Assert.Equal(0, zapomniany.Permisje); // Uprawnienia zresetowane do 0
            Assert.NotEqual("jan.kowalski", zapomniany.Login); // Login zmieniony
            Assert.NotEqual("Jan", zapomniany.Imie); // Imię zamazane
            Assert.NotEqual("jan@test.pl", zapomniany.Email); // Email zamazany
        }
    }
}