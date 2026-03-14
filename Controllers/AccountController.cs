using Microsoft.AspNetCore.Mvc;
using przychodnia.Models;
using przychodnia.Services;

namespace przychodnia.Controllers
{
    public class AccountController : Controller
    {
        // Udawana baza danych na start (potem zamienimy na DBContext)
        private static List<Uzytkownik> _mockDb = new List<Uzytkownik>
        {
            new Uzytkownik { Login="admin", Haslo=PasswordHasher.HashPassword("admin"), Permisje=1 },
            new Uzytkownik { Login="user", Haslo=PasswordHasher.HashPassword("user"), Permisje=0 },
            new Uzytkownik { Login="pracownik", Haslo=PasswordHasher.HashPassword("pracownik"), Permisje=2 }
        };

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string login, string password)
        {
            var hashed = PasswordHasher.HashPassword(password);
            var user = _mockDb.FirstOrDefault(u => u.Login == login && u.Haslo == hashed && u.CzyAktywny);

            if (user != null)
            {
                if (user.Permisje == 1) return RedirectToAction("AdminPanel");
                return Content("Zalogowano jako użytkownik");
            }

            ViewBag.Error = "Błędny login lub hasło";
            return View();
        }

        public IActionResult AdminPanel()
        {
            return View(_mockDb);
        }

        public IActionResult Podglad(int id)
        {
            // Szukamy użytkownika o konkretnym ID w naszej udawanej bazie
            var user = _mockDb.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // Wyświetla pusty formularz
        [HttpGet]
        public IActionResult DodajUzytkownik()
        {
            return View();
        }

        // Odbiera dane z formularza
        [HttpPost]
        public IActionResult DodajUzytkownik(Uzytkownik nowyUzytkownik)
        {
            // Prosta automatyzacja: ID (ostatnie + 1)
            nowyUzytkownik.Id = _mockDb.Max(u => u.Id) + 1;

            // Szyfrujemy hasło przed "zapisem"
            nowyUzytkownik.Haslo = PasswordHasher.HashPassword(nowyUzytkownik.Haslo);

            // Prosta logika wyciągania daty z PESEL (uproszczona na potrzeby testu)
            // Zakładamy format YYMMDD...
            if (nowyUzytkownik.Pesel.Length >= 6)
            {
                int rok = int.Parse(nowyUzytkownik.Pesel.Substring(0, 2));
                int miesiac = int.Parse(nowyUzytkownik.Pesel.Substring(2, 2));
                int dzien = int.Parse(nowyUzytkownik.Pesel.Substring(4, 2));

                // Uproszczenie dla osób urodzonych po 2000 (miesiąc + 20)
                int pelnyRok = (miesiac > 12) ? 2000 + rok : 1900 + rok;
                int pelnyMiesiac = (miesiac > 12) ? miesiac - 20 : miesiac;

                try
                {
                    nowyUzytkownik.DataUrodzenia = new DateTime(pelnyRok, pelnyMiesiac, dzien);
                }
                catch
                {
                    nowyUzytkownik.DataUrodzenia = DateTime.Now;
                }
            }

            _mockDb.Add(nowyUzytkownik);

            // Po dodaniu wracamy do listy
            return RedirectToAction("AdminPanel");
        }


    }
}