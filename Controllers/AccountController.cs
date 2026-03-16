using Microsoft.AspNetCore.Mvc;
using przychodnia.Models;
using przychodnia.Services;
using przychodnia.Data; // Musisz mieć tu swój DbContext
using System.Linq;

namespace przychodnia.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Konstruktor, który wstrzykuje bazę danych
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

            // Dodajemy warunek: u.CzyAktywny == true
            var user = _context.Uzytkownicy.FirstOrDefault(u =>
                u.Login == login &&
                u.Haslo == hashed &&
                u.CzyAktywny == true);

            if (user != null)
            {
                if (user.Permisje == 1) return RedirectToAction("AdminPanel");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Błędny login, hasło lub konto jest nieaktywne";
            return View();
        }

        public IActionResult AdminPanel()
        {
            // Pobieramy wszystkich użytkowników prosto z bazy do tabeli
            var listaUzytkownikow = _context.Uzytkownicy.ToList();
            return View(listaUzytkownikow);
        }

        public IActionResult Podglad(int id)
        {
            // Szukamy w bazie po ID (klucz główny)
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult DodajUzytkownik() => View();

        [HttpPost]
        public IActionResult DodajUzytkownik(Uzytkownik nowyUzytkownik)
        {
            // Haszujemy hasło przed zapisem do bazy
            nowyUzytkownik.Haslo = PasswordHasher.HashPassword(nowyUzytkownik.Haslo);

            // Logika wyciągania daty urodzenia z PESEL (BirthDate)
            if (!string.IsNullOrEmpty(nowyUzytkownik.Pesel) && nowyUzytkownik.Pesel.Length >= 6)
            {
                try
                {
                    int rok = int.Parse(nowyUzytkownik.Pesel.Substring(0, 2));
                    int miesiac = int.Parse(nowyUzytkownik.Pesel.Substring(2, 2));
                    int dzien = int.Parse(nowyUzytkownik.Pesel.Substring(4, 2));

                    int pelnyRok = (miesiac > 12) ? 2000 + rok : 1900 + rok;
                    int pelnyMiesiac = (miesiac > 12) ? miesiac - 20 : miesiac;

                    nowyUzytkownik.DataUrodzenia = new DateTime(pelnyRok, pelnyMiesiac, dzien);
                }
                catch
                {
                    nowyUzytkownik.DataUrodzenia = DateTime.Now;
                }
            }

            // Zapis do prawdziwej bazy projekt.db
            _context.Uzytkownicy.Add(nowyUzytkownik);
            _context.SaveChanges();
            return RedirectToAction("AdminPanel");
        }
            public IActionResult Zapomnij(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);

            if (user != null)
            {
                user.CzyAktywny = false; // Zmieniamy status
                _context.SaveChanges();  // Zapisujemy w projekt.db
            }

            return RedirectToAction("AdminPanel");
        }


    }

}