using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Data;
using przychodnia.Models;
using przychodnia.Services;
using System.Linq;
using System.Security.Cryptography;

namespace przychodnia.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string login, string password)
        {
            var hashed = PasswordHasher.HashPassword(password);
            var userCheck = _context.Uzytkownicy.FirstOrDefault(user => user.Login == login);

            if (userCheck == null || userCheck.Haslo != hashed || !userCheck.CzyAktywny)
            {
                ViewBag.Error = "Błędny login, hasło lub konto nieaktywne.";
                return View();
            }

            // ustawienia ciasteczek
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = System.DateTimeOffset.UtcNow.AddHours(8),
                IsEssential = true
            };

            // logowanie uzytkownika
            Response.Cookies.Append("AuthUser", userCheck.Login, cookieOptions);
            Response.Cookies.Append("AuthUserId", userCheck.ID.ToString(), cookieOptions);

            // przekierowanie po roli
            if ((userCheck.Permisje & 1) != 0)
                return RedirectToAction("AdminPanel");

            if ((userCheck.Permisje & 2) != 0)
                return RedirectToAction("PracownikPanel");

            if ((userCheck.Permisje & 4) != 0)
                return RedirectToAction("PacjentPanel");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult PracownikPanel()
        {
            return View();
        }

        public IActionResult PacjentPanel()
        {
            return View();
        }

        public IActionResult AdminPanel(string searchString, bool showForgotten = false)
        {
            var uzytkownicy = _context.Uzytkownicy.AsQueryable();

            // filtr aktywnych uzytkownikow
            if (showForgotten)
            {
                uzytkownicy = uzytkownicy.Where(uzytkownik => !uzytkownik.CzyAktywny);
            }
            else
            {
                uzytkownicy = uzytkownicy.Where(uzytkownik => uzytkownik.CzyAktywny);
            }

            // wyszukiwarka
            if (!string.IsNullOrEmpty(searchString))
            {
                uzytkownicy = uzytkownicy.Where(search => search.Nazwisko.Contains(searchString)
                                                  || search.Pesel.Contains(searchString));
            }

            ViewBag.ShowForgotten = showForgotten;
            ViewBag.CurrentFilter = searchString;

            return View(uzytkownicy.ToList());
        }

        public IActionResult Podglad(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(user => user.ID == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult DodajUzytkownik() => View();

        [HttpGet]
        public IActionResult Rejestracja() => View();

        [HttpGet]
        public IActionResult Continue()
        {
            // sprawdzenie sesji
            var idCookie = Request.Cookies["AuthUserId"];
            if (string.IsNullOrEmpty(idCookie))
                return RedirectToAction("Login");

            if (!int.TryParse(idCookie, out int userId))
            {
                Response.Cookies.Delete("AuthUser");
                Response.Cookies.Delete("AuthUserId");
                return RedirectToAction("Login");
            }

            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == userId);
            if (user == null || !user.CzyAktywny)
            {
                Response.Cookies.Delete("AuthUser");
                Response.Cookies.Delete("AuthUserId");
                return RedirectToAction("Login");
            }

            // powrot do panelu
            if ((user.Permisje & 1) != 0)
                return RedirectToAction("AdminPanel");

            if ((user.Permisje & 2) != 0)
                return RedirectToAction("PracownikPanel");

            if ((user.Permisje & 4) != 0)
                return RedirectToAction("PacjentPanel");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // wylogowanie
            Response.Cookies.Delete("AuthUser");
            Response.Cookies.Delete("AuthUserId");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DodajUzytkownik(Uzytkownik nowyUzytkownik)
        {
            if (!ModelState.IsValid)
            {
                return View(nowyUzytkownik);
            }

            // sprawdzanie hasla
            if (string.IsNullOrWhiteSpace(nowyUzytkownik.Haslo))
            {
                ModelState.AddModelError("Haslo", "Hasło jest wymagane");
                return View(nowyUzytkownik);
            }

            // unikalnosc danych
            if (_context.Uzytkownicy.Any(u => u.Login == nowyUzytkownik.Login))
                ModelState.AddModelError("Login", "Login już istnieje");

            if (_context.Uzytkownicy.Any(u => u.Pesel == nowyUzytkownik.Pesel))
                ModelState.AddModelError("Pesel", "PESEL już istnieje w systemie");

            if (_context.Uzytkownicy.Any(u => u.Email == nowyUzytkownik.Email))
                ModelState.AddModelError("Email", "E-mail już istnieje w systemie");

            if (!ModelState.IsValid)
                return View(nowyUzytkownik);

            // walidacja pesel
            if (!TryValidatePesel(nowyUzytkownik.Pesel, nowyUzytkownik.Plec, out DateTime dob, out string peselError))
            {
                ModelState.AddModelError("Pesel", peselError);
                return View(nowyUzytkownik);
            }

            // zapis uzytkownika
            nowyUzytkownik.DataUrodzenia = dob;
            nowyUzytkownik.Haslo = PasswordHasher.HashPassword(nowyUzytkownik.Haslo);

            _context.Uzytkownicy.Add(nowyUzytkownik);
            _context.SaveChanges();
            return RedirectToAction("AdminPanel");
        }

        private bool TryValidatePesel(string pesel, string plec, out DateTime dateOfBirth, out string error)
        {
            dateOfBirth = default;
            error = string.Empty;

            // dlugosc pesel
            if (string.IsNullOrEmpty(pesel) || pesel.Length != 11 || !pesel.All(char.IsDigit))
            {
                error = "PESEL nieprawidłowy – niepoprawna data";
                return false;
            }

            // parsuj date
            int year = int.Parse(pesel.Substring(0, 2));
            int month = int.Parse(pesel.Substring(2, 2));
            int day = int.Parse(pesel.Substring(4, 2));

            int fullYear;
            int realMonth = month;

            // wiek i stulecie
            if (month >= 1 && month <= 12)
                fullYear = 1900 + year;
            else if (month >= 21 && month <= 32)
            {
                fullYear = 2000 + year;
                realMonth = month - 20;
            }
            else if (month >= 41 && month <= 52)
            {
                fullYear = 2100 + year;
                realMonth = month - 40;
            }
            else if (month >= 61 && month <= 72)
            {
                fullYear = 2200 + year;
                realMonth = month - 60;
            }
            else if (month >= 81 && month <= 92)
            {
                fullYear = 1800 + year;
                realMonth = month - 80;
            }
            else
            {
                error = "PESEL nieprawidłowy – niepoprawna data";
                return false;
            }

            // tworzenie daty
            try
            {
                dateOfBirth = new DateTime(fullYear, realMonth, day);
            }
            catch
            {
                error = "PESEL nieprawidłowy – niepoprawna data";
                return false;
            }

            // walidacja plci
            int genderDigit = int.Parse(pesel.Substring(9, 1));
            bool isMale = (genderDigit % 2) == 1;
            bool modelSaysMale = (plec ?? string.Empty).ToLower().Contains("m");

            if (modelSaysMale != isMale)
            {
                error = "PESEL nieprawidłowy – niepoprawna płeć";
                return false;
            }

            // cyfra kontrolna
            int[] weights = new[] { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                sum += weights[i] * (pesel[i] - '0');
            }
            int control = (10 - (sum % 10)) % 10;
            int lastDigit = pesel[10] - '0';
            if (control != lastDigit)
            {
                error = $"Błąd! Wyliczona: {control}, w PESEL jest: {lastDigit}";
                return false;
            }
            
            return true;
        }

        public IActionResult Zapomnij(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);

            if (user == null || !user.CzyAktywny)
                return RedirectToAction("AdminPanel");

            // anonimizacja danych
            user.Login = GenerateUniqueLogin();
            user.Email = GenerateUniqueEmail();
            user.Telefon = GenerateRandomDigits(9);

            var genderIsMale = RandomNumberGenerator.GetInt32(0, 2) == 1;
            user.Plec = genderIsMale ? "Mężczyzna" : "Kobieta";

            var dob = GenerateRandomDateOfBirth(18, 90);
            user.DataUrodzenia = dob;

            // generowanie pesel
            string pesel;
            int attempts = 0;
            do
            {
                pesel = GeneratePeselFor(dob, genderIsMale);
                attempts++;
            } while (_context.Uzytkownicy.Any(u => u.Pesel == pesel) && attempts < 10);
            user.Pesel = pesel;

            user.Imie = "Anonim" + RandomString(6);
            user.Nazwisko = "Uzytkownik" + RandomString(6);
            user.Permisje = 0;
            user.CzyAktywny = false;
            user.Haslo = PasswordHasher.HashPassword(Guid.NewGuid().ToString());

            _context.SaveChanges();

            // czyszczenie ciasteczek
            Response.Cookies.Delete("AuthUser");
            Response.Cookies.Delete("AuthUserId");

            return RedirectToAction("AdminPanel");
        }

        private static string GenerateRandomDigits(int length)
        {
            // losowe cyfry
            var sb = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(RandomNumberGenerator.GetInt32(0, 10).ToString());
            return sb.ToString();
        }

        private static string RandomString(int length)
        {
            // losowy tekst
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[RandomNumberGenerator.GetInt32(0, chars.Length)]);
            return sb.ToString();
        }

        private string GenerateUniqueLogin()
        {
            // unikalny login
            string login;
            int attempts = 0;
            do
            {
                login = "deleted_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                attempts++;
            } while (_context.Uzytkownicy.Any(u => u.Login == login) && attempts < 10);
            return login;
        }

        private string GenerateUniqueEmail()
        {
            // unikalny email
            string email;
            int attempts = 0;
            do
            {
                email = $"deleted_{Guid.NewGuid():N}@example.invalid";
                attempts++;
            } while (_context.Uzytkownicy.Any(u => u.Email == email) && attempts < 10);
            return email;
        }

        private DateTime GenerateRandomDateOfBirth(int minAge, int maxAge)
        {
            // losowa data
            var today = DateTime.Today;
            int age = RandomNumberGenerator.GetInt32(minAge, maxAge + 1);
            int year = today.Year - age;
            int month = RandomNumberGenerator.GetInt32(1, 13);
            int day = RandomNumberGenerator.GetInt32(1, DateTime.DaysInMonth(year, month) + 1);
            return new DateTime(year, month, day);
        }

        private string GeneratePeselFor(DateTime dob, bool isMale)
        {
            // losowy pesel
            int year = dob.Year;
            int month = dob.Month;
            int day = dob.Day;

            int monthCode = month;
            int yearTwo = year % 100;

            if (year >= 2000 && year <= 2099) monthCode = month + 20;
            else if (year >= 2100 && year <= 2199) monthCode = month + 40;
            else if (year >= 2200 && year <= 2299) monthCode = month + 60;
            else if (year >= 1800 && year <= 1899) monthCode = month + 80;

            string part = yearTwo.ToString("D2") + monthCode.ToString("D2") + day.ToString("D2");

            var digits = new int[11];
            for (int i = 0; i < 6; i++) digits[i] = part[i] - '0';

            for (int i = 6; i <= 8; i++)
                digits[i] = RandomNumberGenerator.GetInt32(0, 10);

            int genderDigit = RandomNumberGenerator.GetInt32(0, 10);
            if (isMale && genderDigit % 2 == 0) genderDigit = (genderDigit + 1) % 10;
            if (!isMale && genderDigit % 2 == 1) genderDigit = (genderDigit + 1) % 10;
            digits[9] = genderDigit;

            int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
            int sum = 0;
            for (int i = 0; i < 10; i++) sum += weights[i] * digits[i];
            digits[10] = (10 - (sum % 10)) % 10;

            var sb = new System.Text.StringBuilder(11);
            for (int i = 0; i < 11; i++) sb.Append(digits[i]);
            return sb.ToString();
        }

        public IActionResult ListaUprawnien()
        {
            // lista ról
            var uprawnienia = new List<dynamic>
            {
                new { ID = 1, Nazwa = "Administrator", Opis = "Pełny dostęp." },
                new { ID = 2, Nazwa = "Pracownik", Opis = "Dostęp operacyjny." },
                new { ID = 4, Nazwa = "Użytkownik", Opis = "Uprawnienia pacjenta." }
            };
            return View(uprawnienia);
        }

        [HttpGet]
        public IActionResult Uprawnienia(int id)
        {
            // formularz uprawnien
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Uprawnienia(int id, int[] wybraneRole)
        {
            // zmiana uprawnien
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();

            user.Permisje = 0;
            if (wybraneRole == null || wybraneRole.Length == 0)
            {
                ViewBag.Error = "nie zaznaczono żadnych uprawnień";
                return View(user);
            }

            user.Permisje = wybraneRole.Sum();
            _context.SaveChanges();
            return RedirectToAction("Podglad", new { id = user.ID });
        }

        public IActionResult UzytkownicyZUprawnieniem(int id)
        {
            // filtr po uprawnieniu
            var wszyscyAktywni = _context.Uzytkownicy.AsNoTracking().Where(u => u.CzyAktywny).ToList();
            var przefiltrowani = wszyscyAktywni.Where(u => (u.Permisje & id) != 0).ToList();

            ViewBag.Rola = id == 1 ? "Administrator" : id == 2 ? "Pracownik" : "Pacjent";
            return View(przefiltrowani);
        }

        [HttpGet]
        public IActionResult Edytuj(int id)
        {
            // edycja profilu
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edytuj(Uzytkownik model)
        {
            // zapis edycji
            if (!ModelState.IsValid)
                return View(model);

            var uzytkownik = _context.Uzytkownicy.FirstOrDefault(u => u.ID == model.ID);
            if (uzytkownik == null) return NotFound();

            uzytkownik.Imie = model.Imie;
            uzytkownik.Nazwisko = model.Nazwisko;
            uzytkownik.Email = model.Email;
            uzytkownik.Pesel = model.Pesel;

            _context.SaveChanges();

            var currentLogin = Request.Cookies["AuthUser"];
            if (!string.IsNullOrEmpty(currentLogin) && currentLogin == uzytkownik.Login)
                Response.Cookies.Append("AuthUser", uzytkownik.Login, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });

            return RedirectToAction("Podglad", new { id = uzytkownik.ID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rejestracja(Uzytkownik model)
        {
            // obsluga rejestracji
            ModelState.Remove("Plec");
            ModelState.Remove("DataUrodzenia");

            if (!string.IsNullOrEmpty(model.Pesel) && model.Pesel.Length == 11)
            {
                int genderDigit = int.Parse(model.Pesel.Substring(9, 1));
                model.Plec = (genderDigit % 2 == 1) ? "Mężczyzna" : "Kobieta";

                if (TryValidatePesel(model.Pesel, model.Plec, out DateTime dob, out string peselError))
                    model.DataUrodzenia = dob;
                else
                    ModelState.AddModelError("Pesel", peselError);
            }

            if (ModelState.IsValid)
            {
                if (_context.Uzytkownicy.Any(u => u.Login == model.Login || u.Pesel == model.Pesel || u.Email == model.Email))
                {
                    ViewBag.Error = "Użytkownik o podanym loginie, PESEL lub e-mail już istnieje.";
                    return View(model);
                }

                model.Haslo = PasswordHasher.HashPassword(model.Haslo);
                model.Permisje = 4;
                model.CzyAktywny = true;

                _context.Uzytkownicy.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }
            return View(model);
        }
    }
}