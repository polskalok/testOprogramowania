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

        [HttpGet]
        public IActionResult OdzyskajHaslo() => View();

        [HttpPost]
        public async Task<IActionResult> OdzyskajHaslo(string login, string email)
        {
            
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.Login == login && u.Email == email);

            if (user == null || !user.CzyAktywny)
            {
                
                ViewBag.Error = "Podane dane nie pasują do żadnego konta w systemie.";
                return View();
            }

            
            string newPassword = PasswordHasher.GenerateRandomPassword();

         
            bool emailSent = await EmailService.SendPasswordRecoveryEmail(user.Email, newPassword);

            if (!emailSent)
            {
                ViewBag.Error = "Nie udało się wysłać e-maila. Spróbuj ponownie później.";
                return View();
            }

            
            string hashedPassword = PasswordHasher.HashPassword(newPassword);
            user.Haslo = hashedPassword;
            user.MuszZmieniHaslo = true;
            await _context.SaveChangesAsync();

            
            return RedirectToAction("Login", new { recovered = true });
        }

        [HttpPost]
        public IActionResult Login(string login, string password)
        {
            var hashed = PasswordHasher.HashPassword(password?.Trim() ?? "");

            var userCheck = _context.Uzytkownicy.FirstOrDefault(user => user.Login == login);

            if (userCheck == null)
            {
                
                ViewBag.Error = "Błędne hasło lub login";
                return View();
            }

            if (userCheck.LockoutEnd != null && userCheck.LockoutEnd > DateTime.Now)
            {
                ViewBag.Error = $"Konto zostało zablokowane czasowo po trzech nieudanych próbach do: {userCheck.LockoutEnd.Value.ToString("HH:mm")}";
                return View();
            }

            if (userCheck.Haslo != hashed || !userCheck.CzyAktywny)
            {
                userCheck.FailedLoginAttempts++;

                if (userCheck.FailedLoginAttempts >= 3)
                {
                    userCheck.LockoutEnd = DateTime.Now.AddHours(2);
                    _context.SaveChanges();
                    ViewBag.Error = $"Konto zostało zablokowane czasowo po trzech nieudanych próbach do: {userCheck.LockoutEnd.Value.ToString("HH:mm")}";
                    return View();
                }

                _context.SaveChanges();
                
                ViewBag.Error = "Błędne hasło lub login";
                return View();
            }

            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = System.DateTimeOffset.UtcNow.AddHours(8),
                IsEssential = true
            };

            Response.Cookies.Append("AuthUser", userCheck.Login, cookieOptions);
            Response.Cookies.Append("AuthUserId", userCheck.ID.ToString(), cookieOptions);

            userCheck.FailedLoginAttempts = 0;
            userCheck.LockoutEnd = null;
            _context.SaveChanges();

            if (userCheck.MuszZmieniHaslo)
            {
                Response.Cookies.Append("MuszZmieniHaslo", "true", cookieOptions);
                return RedirectToAction("MuszZmieniHaslo", new { id = userCheck.ID });
            }

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

        [HttpGet]
        public IActionResult AdminPanel(string searchString, bool showForgotten = false)
        {
           
            if (HttpContext != null)
            {
                var login = HttpContext.Request.Cookies["AuthUser"];
                if (string.IsNullOrEmpty(login)) return RedirectToAction("Login");

                var user = _context.Uzytkownicy.FirstOrDefault(u => u.Login == login);
                if (user == null || (user.Permisje & 1) == 0) return Unauthorized();
            }

            var query = _context.Uzytkownicy.AsQueryable();

            
            if (showForgotten)
            {
                query = query.Where(u => !u.CzyAktywny);
            }
            else
            {
                query = query.Where(u => u.CzyAktywny);
            }

            
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string termLower = searchString.Trim().ToLower();
                string phoneSearchTerm = termLower.Replace("+", "");

                var parts = termLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    string p1 = parts[0];
                    string p2 = parts[1];

                    query = query.Where(u =>
                        (u.Imie != null && u.Nazwisko != null && ((u.Imie.ToLower().Contains(p1) && u.Nazwisko.ToLower().Contains(p2)) || (u.Imie.ToLower().Contains(p2) && u.Nazwisko.ToLower().Contains(p1)))) ||
                        (u.Adres != null && u.Adres.ToLower().Contains(termLower)) ||
                        (u.Login != null && u.Login.ToLower().Contains(termLower)) ||
                        (u.Email != null && u.Email.ToLower().Contains(termLower))
                    );
                }
                else
                {
                    query = query.Where(u =>
                        (u.Imie != null && u.Imie.ToLower().Contains(termLower)) ||
                        (u.Nazwisko != null && u.Nazwisko.ToLower().Contains(termLower)) ||
                        (u.Pesel != null && u.Pesel.Contains(termLower)) ||
                        (u.Adres != null && u.Adres.ToLower().Contains(termLower)) ||
                        (u.Telefon != null && (u.Telefon.Contains(termLower) || u.Telefon.Contains(phoneSearchTerm))) ||
                        (u.Login != null && u.Login.ToLower().Contains(termLower)) ||
                        (u.Email != null && u.Email.ToLower().Contains(termLower))
                    );
                }
            }

            ViewBag.CurrentFilter = searchString;
            ViewBag.ShowForgotten = showForgotten;

            return View(query.ToList());
        }
        public IActionResult Podglad(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(user => user.ID == id);

           
            if (user == null || !user.CzyAktywny)
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
            
            Response.Cookies.Delete("AuthUser");
            Response.Cookies.Delete("AuthUserId");

            
            return RedirectToAction("Login", "Account");
        }

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

            
            if (!PasswordHasher.ValidatePassword(nowyUzytkownik.Haslo, out string passwordError))
            {
                ModelState.AddModelError("Haslo", passwordError);
                return View(nowyUzytkownik);
            }

            
            if (_context.Uzytkownicy.Any(u => u.Login == nowyUzytkownik.Login))
                ModelState.AddModelError("Login", "Login już istnieje");

            if (_context.Uzytkownicy.Any(u => u.Pesel == nowyUzytkownik.Pesel))
                ModelState.AddModelError("Pesel", "PESEL już istnieje w systemie");

            if (_context.Uzytkownicy.Any(u => u.Email == nowyUzytkownik.Email))
                ModelState.AddModelError("Email", "E-mail już istnieje w systemie");

            if (!ModelState.IsValid)
                return View(nowyUzytkownik);

            
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

        private bool TryValidatePesel(string pesel, string? plec, out DateTime dateOfBirth, out string error)
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
                error = $"PESEL nieprawidłowy - niepoprawna cyfra kontrolna";
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
        // DODANE PARAMETRY: typPracownika i specjalizacja
        public IActionResult Uprawnienia(int id, int[] wybraneRole, string typPracownika, string specjalizacja)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();

            if (user.Login != null && user.Login.Contains("zamazany_login"))
            {
                ViewBag.Error = "Nie można edytować uprawnień zapomnianego użytkownika.";
                return View(user);
            }

            user.Permisje = 0;
            if (wybraneRole == null || wybraneRole.Length == 0)
            {
                ViewBag.Error = "nie zaznaczono żadnych uprawnień";
                return View(user);
            }

            int nowePermisje = wybraneRole.Sum();
            user.Permisje = nowePermisje;

            // --- NOWA LOGIKA: Zarządzanie pod-uprawnieniami Pracownika (Maska bitowa = 2) ---
            if ((nowePermisje & 2) != 0)
            {
                if (typPracownika == "Lekarz")
                {
                    if (string.IsNullOrEmpty(specjalizacja))
                    {
                        ViewBag.Error = "Wybierz specjalizację dla lekarza.";
                        return View(user);
                    }
                    user.Specjalizacja = specjalizacja; // Zapisanie specjalizacji
                }
                else
                {
                    // Recepcjonista - wymuszamy brak specjalizacji
                    user.Specjalizacja = null;
                }
            }
            else
            {
                // Jeśli admin całkowicie odbierze uprawnienia pracownika, czyścimy specjalizację z bazy
                user.Specjalizacja = null;
            }
            // ---------------------------------------------------------------------------------

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
            
            var edytowanePola = new[] { "Imie", "Nazwisko", "Email", "Pesel" };
            var polaDoUsuniecia = ModelState.Keys.Where(k => !edytowanePola.Contains(k)).ToList();

            foreach (var pole in polaDoUsuniecia)
            {
                ModelState.Remove(pole);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var uzytkownik = _context.Uzytkownicy.FirstOrDefault(u => u.ID == model.ID);
            if (uzytkownik == null) return NotFound();

            if (!string.IsNullOrEmpty(model.Email) && uzytkownik.Email != model.Email)
            {
                if (_context.Uzytkownicy.Any(u => u.Email == model.Email && u.ID != model.ID))
                {
                    ModelState.AddModelError("Email", "Adres e-mail już istnieje w systemie");
                    return View(model);
                }
            }

            if (!string.IsNullOrEmpty(model.Pesel) && uzytkownik.Pesel != model.Pesel)
            {
                if (!TryValidatePesel(model.Pesel, null, out DateTime dob, out string peselError))
                {
                    ModelState.AddModelError("Pesel", peselError);
                    return View(model);
                }

                if (_context.Uzytkownicy.Any(u => u.Pesel == model.Pesel && u.ID != model.ID))
                {
                    ModelState.AddModelError("Pesel", "PESEL już istnieje w systemie");
                    return View(model);
                }
            }

            
            uzytkownik.Imie = model.Imie;
            uzytkownik.Nazwisko = model.Nazwisko;
            uzytkownik.Email = model.Email;
            uzytkownik.Pesel = model.Pesel;

            
            _context.Update(uzytkownik);
            _context.SaveChanges();

            
            if (TempData != null)
            {
                TempData["SuccessMessage"] = "Sukces! Zaktualizowano poprawnie dane.";
            }

            var currentLogin = Request.Cookies["AuthUser"];
            if (!string.IsNullOrEmpty(currentLogin) && currentLogin == uzytkownik.Login)
            {
                Response.Cookies.Append("AuthUser", uzytkownik.Login, new CookieOptions { HttpOnly = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax });
            }

            return RedirectToAction("Podglad", new { id = uzytkownik.ID });
        }

        [HttpGet]
        public IActionResult ZmieniHaslo(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ZmieniHaslo(int id, string noweHaslo)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();

            if (!PasswordHasher.ValidatePassword(noweHaslo, out string errorMessage))
            {
                ViewBag.Error = errorMessage;
                return View(user);
            }

            user.Haslo = PasswordHasher.HashPassword(noweHaslo);
            _context.SaveChanges();

            ViewBag.Success = "Hasło zostało zmienione pomyślnie.";
            return RedirectToAction("Podglad", new { id = user.ID });
        }

        [HttpGet]
        public IActionResult MuszZmieniHaslo(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MuszZmieniHaslo(int id, string noweHaslo, string powtorzHaslo)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);
            if (user == null) return NotFound();

            
            if (noweHaslo != powtorzHaslo)
            {
                ViewBag.Error = "Upewnij się że hasła są identyczne";
                return View(user);
            }

            // walidacja hasła
            if (!PasswordHasher.ValidatePassword(noweHaslo, out string errorMessage))
            {
                ViewBag.Error = errorMessage;
                return View(user);
            }

           
            if (!string.IsNullOrEmpty(user.OstatniaHasla))
            {
                var ostatniaHasla = user.OstatniaHasla.Split('|');
                string newHashedPassword = PasswordHasher.HashPassword(noweHaslo);

                foreach (var stareHaslo in ostatniaHasla)
                {
                    if (stareHaslo == newHashedPassword)
                    {
                        ViewBag.Error = "Nowe hasło musi różnić się od 3 ostatnich używanych haseł.";
                        return View(user);
                    }
                }
            }

            
            string nowszaHistoria = PasswordHasher.HashPassword(noweHaslo);
            if (!string.IsNullOrEmpty(user.OstatniaHasla))
            {
                var ostatnie = user.OstatniaHasla.Split('|').Take(2).ToList();
                nowszaHistoria = nowszaHistoria + "|" + string.Join("|", ostatnie);
            }
            user.OstatniaHasla = nowszaHistoria;

            // zmiana hasła
            user.Haslo = PasswordHasher.HashPassword(noweHaslo);
            user.MuszZmieniHaslo = false;
            _context.SaveChanges();

            
            Response.Cookies.Delete("MuszZmieniHaslo");

            ViewBag.Success = "Hasło zostało zmienione pomyślnie. Możesz się teraz zalogować.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rejestracja(Uzytkownik model)
        {
            ModelState.Remove("Plec");
            ModelState.Remove("DataUrodzenia");

            if (!PasswordHasher.ValidatePassword(model.Haslo, out string passwordError))
            {
                ModelState.AddModelError("Haslo", passwordError);
            }

            if (!string.IsNullOrEmpty(model.Pesel) && model.Pesel.Length == 11)
            {
                int genderDigit = int.Parse(model.Pesel.Substring(9, 1));
                model.Plec = (genderDigit % 2 == 1) ? "Mężczyzna" : "Kobieta";

                if (TryValidatePesel(model.Pesel, model.Plec, out DateTime dob, out string peselError))
                {
                    model.DataUrodzenia = dob;
                }
                else
                {
                    ModelState.AddModelError("Pesel", peselError);
                }
            }
            else if (string.IsNullOrEmpty(model.Pesel))
            {
                ModelState.AddModelError("Pesel", "Numer PESEL jest wymagany.");
            }

            if (_context.Uzytkownicy.Any(u => u.Login == model.Login))
                ModelState.AddModelError("Login", "Ten login jest już zajęty.");

            if (_context.Uzytkownicy.Any(u => u.Email == model.Email))
                ModelState.AddModelError("Email", "Ten adres e-mail jest już zarejestrowany.");

            if (_context.Uzytkownicy.Any(u => u.Pesel == model.Pesel))
                ModelState.AddModelError("Pesel", "Użytkownik z tym numerem PESEL już istnieje.");

            if (ModelState.IsValid)
            {
                model.Haslo = PasswordHasher.HashPassword(model.Haslo);
                model.Permisje = 4;
                model.CzyAktywny = true;

                _context.Uzytkownicy.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Login", new { success = true });
            }

            return View(model);
        }

        public IActionResult ZmieniHaslo(int id, string noweHaslo, string powtorzHaslo)
        {
            if (noweHaslo != powtorzHaslo)
            {
                ViewBag.Error = "Upewnij się że hasła są identyczne";
                return View();
            }

           
            return ZmieniHaslo(id, noweHaslo);
        }




        //nowe



        // --- DODAWANIE PACJENTA (Tylko wymagane pola) ---
        [HttpGet]
        public IActionResult PracownikDodaj()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PracownikDodaj(Pacjent model)
        {
            // Wykonujemy tylko walidację formatu PESEL (czy ma 11 cyfr i czy suma kontrolna gra)
            if (!string.IsNullOrEmpty(model.Pesel) && model.Pesel.Length == 11)
            {
                // 1. Wyliczamy płeć na podstawie PESEL
                int genderDigit = int.Parse(model.Pesel.Substring(9, 1));
                string wyliczonaPlec = (genderDigit % 2 == 1) ? "Mężczyzna" : "Kobieta";

                // 2. Przesyłamy wyliczoną płeć do walidatora. 
                // Teraz 'actualGender' będzie zawsze równe 'expectedGender' i błąd zniknie.
                if (!TryValidatePesel(model.Pesel, wyliczonaPlec, out _, out string peselError))
                {
                    ModelState.AddModelError("Pesel", peselError);
                }
            }

            if (ModelState.IsValid)
            {
                // Sprawdzamy czy taki PESEL już jest w tabeli Pacjenci
                if (_context.Pacjenci.Any(p => p.Pesel == model.Pesel))
                {
                    ViewBag.Error = "Pacjent o tym numerze PESEL jest już w bazie.";
                    return View(model);
                }

                // Zapisujemy tylko te pola, które masz w bazie Pacjenci
                _context.Pacjenci.Add(model);
                _context.SaveChanges();

                TempData["Success"] = $"Pacjent {model.Imie} {model.Nazwisko} dodany pomyślnie.";
                return RedirectToAction("PracownikLista");
            }

            return View(model);
        }

        // --- PODGLĄD PACJENTA ---
        [HttpGet]
        public IActionResult PracownikPodglad(int id)
        {
            var pacjent = _context.Pacjenci.FirstOrDefault(p => p.ID == id);
            if (pacjent == null) return NotFound();

            return View("PracownikPodglad", pacjent);
        }

        // --- EDYCJA PACJENTA ---
        [HttpGet]
        public IActionResult EdytujPacjenta(int id)
        {
            var pacjent = _context.Pacjenci.FirstOrDefault(p => p.ID == id);
            if (pacjent == null) return NotFound();

            return View(pacjent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EdytujPacjenta(Pacjent model)
        {
            ModelState.Remove("Plec");
            ModelState.Remove("DataUrodzenia");

            if (!string.IsNullOrEmpty(model.Pesel) && model.Pesel.Length == 11)
            {
                int genderDigit = int.Parse(model.Pesel.Substring(9, 1));
                string wyliczonaPlec = (genderDigit % 2 == 1) ? "Mężczyzna" : "Kobieta";

                if (!TryValidatePesel(model.Pesel, wyliczonaPlec, out _, out string peselError))
                {
                    ModelState.AddModelError("Pesel", peselError);
                }
            }

            bool czyPeselZajety = _context.Pacjenci.Any(p => p.Pesel == model.Pesel && p.ID != model.ID);
            if (czyPeselZajety)
            {
                ModelState.AddModelError("Pesel", "Podany PESEL już widnieje w systemie.");
            }

            bool czyEmailZajety = _context.Pacjenci.Any(p => p.Email == model.Email && p.ID != model.ID);
            if (czyEmailZajety)
            {
                ModelState.AddModelError("Email", "Podany e-mail już widnieje w systemie.");
            }

            if (ModelState.IsValid)
            {
                var dbPacjent = _context.Pacjenci.FirstOrDefault(p => p.ID == model.ID);
                if (dbPacjent != null)
                {
                    dbPacjent.Imie = model.Imie;
                    dbPacjent.Nazwisko = model.Nazwisko;
                    dbPacjent.Email = model.Email;
                    dbPacjent.Pesel = model.Pesel;
                    dbPacjent.Adres = model.Adres;
                    dbPacjent.Telefon = model.Telefon;

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Dane pacjenta zostały zaktualizowane.";
                    return RedirectToAction("PracownikPodglad", new { id = dbPacjent.ID });
                }
            }
            return View(model);
        }


        // zaawansowane wyszukiwanie

        [HttpGet]
        public IActionResult PracownikLista(string searchString)
        {
            var query = _context.Pacjenci.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string termLower = searchString.Trim().ToLower();

                string phoneSearchTerm = termLower.Replace("+", "");

                var parts = termLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    string p1 = parts[0];
                    string p2 = parts[1];

                    query = query.Where(p =>
                        (p.Imie.ToLower().Contains(p1) && p.Nazwisko.ToLower().Contains(p2)) ||
                        (p.Imie.ToLower().Contains(p2) && p.Nazwisko.ToLower().Contains(p1)) ||
                        (p.Adres != null && p.Adres.ToLower().Contains(termLower))
                    );
                }
                else
                {
                    query = query.Where(p =>
                        (p.Imie != null && p.Imie.ToLower().Contains(termLower)) ||
                        (p.Nazwisko != null && p.Nazwisko.ToLower().Contains(termLower)) ||
                        (p.Pesel != null && p.Pesel.Contains(termLower)) ||
                        (p.Adres != null && p.Adres.ToLower().Contains(termLower)) ||
                        (p.Telefon != null && (p.Telefon.Contains(termLower) || p.Telefon.Contains(phoneSearchTerm)))
                    );
                }
            }

            ViewBag.CurrentFilter = searchString;
            return View(query.ToList());
        }
        //zapominanie pacjentow przez pracownika

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ZapomnijPacjenta(int id)
        {
            // Szukamy pacjenta w nowej tabeli kartotek
            var pacjent = _context.Pacjenci.FirstOrDefault(p => p.ID == id);

            if (pacjent == null)
            {
                return NotFound();
            }

            // Usuwamy rekord z bazy danych
            _context.Pacjenci.Remove(pacjent);
            _context.SaveChanges();

            // Komunikat dla pracownika
            TempData["Success"] = "Dane pacjenta zostały trwale usunięte z kartoteki (prawo do zapomnienia).";

            return RedirectToAction("PracownikLista");
        }








    }
}