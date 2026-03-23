using Microsoft.AspNetCore.Mvc;
using przychodnia.Models;
using przychodnia.Services;
using przychodnia.Data; 
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

            
            switch (userCheck.Permisje)
            {
                case 1: // Administrator
                    return RedirectToAction("AdminPanel");
                case 2: // Pracownik
                    return RedirectToAction("PracownikPanel");
                case 0: // Zwykły użytkownik
                    return RedirectToAction("PacjentPanel");
                default:
                    return RedirectToAction("Index", "Home");
            }
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

            if (showForgotten)
            {
                uzytkownicy = uzytkownicy.Where(uzytkownik => !uzytkownik.CzyAktywny);
            }
            else
            {
                uzytkownicy = uzytkownicy.Where(uzytkownik => uzytkownik.CzyAktywny);
            }

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DodajUzytkownik(Uzytkownik nowyUzytkownik)
        {
            if (!ModelState.IsValid)
            {
                return View(nowyUzytkownik);
            }

            if (string.IsNullOrWhiteSpace(nowyUzytkownik.Haslo))
            {
                ModelState.AddModelError("Haslo", "Hasło jest wymagane");
                return View(nowyUzytkownik);
            }

            if (_context.Uzytkownicy.Any(uzytkownik => uzytkownik.Login == nowyUzytkownik.Login))
            {
                ModelState.AddModelError("Login", "Login już istnieje");
            }

            if (_context.Uzytkownicy.Any(uzytkownik => uzytkownik.Pesel == nowyUzytkownik.Pesel))
            {
                ModelState.AddModelError("Pesel", "PESEL już istnieje w systemie");
            }

            if (_context.Uzytkownicy.Any(uzytkownik => uzytkownik.Email == nowyUzytkownik.Email))
            {
                ModelState.AddModelError("Email", "E-mail już istnieje w systemie");
            }

            if (!ModelState.IsValid)
            {
                return View(nowyUzytkownik);
            }

            if (!TryValidatePesel(nowyUzytkownik.Pesel, nowyUzytkownik.Plec, out DateTime dob, out string peselError))
            {
                ModelState.AddModelError("Pesel", peselError);
                return View(nowyUzytkownik);
            }

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

            if (string.IsNullOrEmpty(pesel) || pesel.Length != 11 || !pesel.All(char.IsDigit))
            {
                error = "PESEL nieprawidłowy – niepoprawna data";
                return false;
            }

            int year = int.Parse(pesel.Substring(0, 2));
            int month = int.Parse(pesel.Substring(2, 2));
            int day = int.Parse(pesel.Substring(4, 2));

            int fullYear;
            int realMonth = month;

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

            try
            {
                dateOfBirth = new DateTime(fullYear, realMonth, day);
            }
            catch
            {
                error = "PESEL nieprawidłowy – niepoprawna data";
                return false;
            }

            
            int genderDigit = int.Parse(pesel.Substring(9, 1));
            bool isMale = (genderDigit % 2) == 1;
            bool modelSaysMale = (plec ?? string.Empty).ToLower().Contains("m");

            if (modelSaysMale != isMale)
            {
                error = "PESEL nieprawidłowy – niepoprawna płeć";
                return false;
            }

            
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
                error = "PESEL nieprawidłowy – niepoprawna cyfra kontrolna";
                return false;
            }

            return true;
        }
        public IActionResult Zapomnij(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);

            if (user == null)
                return RedirectToAction("AdminPanel");

            if (!user.CzyAktywny)
                return RedirectToAction("AdminPanel");

            
            user.Login = GenerateUniqueLogin();

            
            user.Email = GenerateUniqueEmail();

            
            user.Telefon = GenerateRandomDigits(9);


            var genderIsMale = RandomNumberGenerator.GetInt32(0, 2) == 1;
            user.Plec = genderIsMale ? "Mężczyzna" : "Kobieta";


            var dob = GenerateRandomDateOfBirth(18, 90);
            user.DataUrodzenia = dob;


            string pesel;
            int attempts = 0;
            do
            {
                pesel = GeneratePeselFor(dob, genderIsMale);
                attempts++;
            } while (_context.Uzytkownicy.Any(uzytkownik => uzytkownik.Pesel == pesel) && attempts < 10);
            user.Pesel = pesel;


            user.Imie = "Anonim" + RandomString(6);
            user.Nazwisko = "Uzytkownik" + RandomString(6);


            user.Permisje = 0;
            user.CzyAktywny = false;


            user.Haslo = PasswordHasher.HashPassword(Guid.NewGuid().ToString());

            _context.SaveChanges();

            return RedirectToAction("AdminPanel");
        }

        private static string GenerateRandomDigits(int length)
        {
            var sb = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(RandomNumberGenerator.GetInt32(0, 10).ToString());
            }
            return sb.ToString();
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[RandomNumberGenerator.GetInt32(0, chars.Length)]);
            }
            return sb.ToString();
        }

        private string GenerateUniqueLogin()
        {
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
            string email;
            int attempts = 0;
            do
            {
                email = $"deleted_{Guid.NewGuid():N}@example.invalid";
                attempts++;
            } while (_context.Uzytkownicy.Any(uzytkownik => uzytkownik.Email == email) && attempts < 10);
            return email;
        }

        private DateTime GenerateRandomDateOfBirth(int minAge, int maxAge)
        {
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
            digits[0] = part[0] - '0';
            digits[1] = part[1] - '0';
            digits[2] = part[2] - '0';
            digits[3] = part[3] - '0';
            digits[4] = part[4] - '0';
            digits[5] = part[5] - '0';

            for (int i = 6; i <= 8; i++)
            {
                digits[i] = RandomNumberGenerator.GetInt32(0, 10);
            }

            
            int genderDigit = RandomNumberGenerator.GetInt32(0, 10);
            if (isMale && genderDigit % 2 == 0) genderDigit = (genderDigit + 1) % 10;
            if (!isMale && genderDigit % 2 == 1) genderDigit = (genderDigit + 1) % 10;
            digits[9] = genderDigit;

            // cyfra kontrolna
            int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
            int sum = 0;
            for (int i = 0; i < 10; i++) sum += weights[i] * digits[i];
            int control = (10 - (sum % 10)) % 10;
            digits[10] = control;

            var sb = new System.Text.StringBuilder(11);
            for (int i = 0; i < 11; i++) sb.Append(digits[i]);
            return sb.ToString();
        }


    }

}