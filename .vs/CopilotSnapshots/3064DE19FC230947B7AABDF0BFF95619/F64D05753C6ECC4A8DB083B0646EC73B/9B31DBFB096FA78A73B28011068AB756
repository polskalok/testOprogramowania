using Microsoft.AspNetCore.Mvc;
using przychodnia.Models;
using przychodnia.Services;
using przychodnia.Data; 
using System.Linq;

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
            var userCheck = _context.Uzytkownicy.FirstOrDefault(u => u.Login == login);

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

        public IActionResult AdminPanel(string searchString)
        {

            var uzytkownicy = from u in _context.Uzytkownicy
                              select u;

       
            if (!string.IsNullOrEmpty(searchString))
            {
               
                uzytkownicy = uzytkownicy.Where(s => s.Nazwisko.Contains(searchString)
                                                  || s.Pesel.Contains(searchString));

              
                ViewBag.CurrentFilter = searchString;
            }

            return View(uzytkownicy.ToList());
        }

        public IActionResult Podglad(int id)
        {
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
            
            if (string.IsNullOrEmpty(nowyUzytkownik.Haslo))
            {
                ViewBag.Error = "Hasło nie może być puste!";
                return View();
            }

            nowyUzytkownik.Haslo = PasswordHasher.HashPassword(nowyUzytkownik.Haslo);
          

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

            _context.Uzytkownicy.Add(nowyUzytkownik);
            _context.SaveChanges();
            return RedirectToAction("AdminPanel");
        }
            public IActionResult Zapomnij(int id)
        {
            var user = _context.Uzytkownicy.FirstOrDefault(u => u.ID == id);

            if (user != null)
            {
                user.CzyAktywny = false; 
                _context.SaveChanges();  
            }

            return RedirectToAction("AdminPanel");
        }


    }

}