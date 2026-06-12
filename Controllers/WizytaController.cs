using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using przychodnia.Data;
using przychodnia.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace przychodnia.Controllers
{
    public class WizytaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WizytaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Rejestruj()
        {
            var idCookie = Request.Cookies["AuthUserId"];
            if (string.IsNullOrEmpty(idCookie) || !int.TryParse(idCookie, out int zalogowanyId))
            {
                return RedirectToAction("Login", "Account");
            }

            var uzytkownik = _context.Uzytkownicy.FirstOrDefault(u => u.ID == zalogowanyId);
            if (uzytkownik == null || !uzytkownik.CzyAktywny || (uzytkownik.Permisje & 2) == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            PrzygotujDaneDoFormularza();
            return View(new Wizyta { DataRozpoczecia = DateTime.Now.AddDays(1) });
        }


        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rejestruj(Wizyta model)
        {
            
            if (model.PacjentID == null || model.PacjentID == 0)
            {
                ModelState.AddModelError("PacjentID", "Wypełnij to pole");
            }
            if (model.LekarzID == null || model.LekarzID == 0)
            {
                ModelState.AddModelError("LekarzID", "Wypełnij to pole");
            }
            if (model.GabinetID == null || model.GabinetID == 0)
            {
                ModelState.AddModelError("GabinetID", "Wypełnij to pole");
            }

            
            model.DataRozpoczecia = new DateTime(
                model.DataRozpoczecia.Year,
                model.DataRozpoczecia.Month,
                model.DataRozpoczecia.Day,
                model.DataRozpoczecia.Hour,
                model.DataRozpoczecia.Minute,
                0, 0
            );

            model.DataZakonczenia = model.DataRozpoczecia.AddMinutes(30);

           
            if (!ModelState.IsValid)
            {
                PrzygotujDaneDoFormularza();
                return View(model);
            }

           
            bool konflikt = _context.Wizyty.Any(w =>
                w.Status == "Zarejestrowana" &&
                (w.GabinetID == model.GabinetID || w.LekarzID == model.LekarzID) &&
                (model.DataRozpoczecia < w.DataZakonczenia && model.DataZakonczenia > w.DataRozpoczecia)
            );

            if (konflikt)
            {
                ModelState.AddModelError("DataRozpoczecia", "Brak wolnych terminów w tym okresie");
                PrzygotujDaneDoFormularza();
                return View(model);
            }

            
            model.Status = "Zarejestrowana";
            _context.Wizyty.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Pomyślnie zarejestrowano wizytę";
            return RedirectToAction("Rejestruj");
        }

        [HttpGet]
        public IActionResult ListaWizyt(string szukajPacjenta, int? szukajLekarza, string szukajSpecjalizacja, DateTime? dataOd, DateTime? dataDo)
        {
            var idCookie = Request.Cookies["AuthUserId"];
            if (string.IsNullOrEmpty(idCookie) || !int.TryParse(idCookie, out int zalogowanyId))
            {
                return RedirectToAction("Login", "Account");
            }

            var uzytkownik = _context.Uzytkownicy.FirstOrDefault(u => u.ID == zalogowanyId);
            if (uzytkownik == null || !uzytkownik.CzyAktywny || (uzytkownik.Permisje & 2) == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            bool czyLekarz = !string.IsNullOrEmpty(uzytkownik.Specjalizacja);
            ViewBag.CzyLekarz = czyLekarz;

            bool czyFormularzWyslany = szukajPacjenta != null || szukajLekarza.HasValue || szukajSpecjalizacja != null || dataOd.HasValue || dataDo.HasValue;


            bool brakKryteriow = string.IsNullOrWhiteSpace(szukajPacjenta) &&
                                 !szukajLekarza.HasValue &&
                                 string.IsNullOrWhiteSpace(szukajSpecjalizacja) &&
                                 !dataOd.HasValue &&
                                 !dataDo.HasValue;

            if (czyFormularzWyslany && brakKryteriow)
            {
                ViewBag.Komunikat = "Wypełnij co najmniej jedno kryterium";

                if (!czyLekarz)
                {
                    ViewBag.WszyscyLekarze = _context.Uzytkownicy.Where(u => (u.Permisje & 2) != 0 && u.CzyAktywny && !string.IsNullOrEmpty(u.Specjalizacja)).ToList();
                    ViewBag.WszystkieSpecjalizacje = _context.Uzytkownicy.Where(u => !string.IsNullOrEmpty(u.Specjalizacja) && u.CzyAktywny).Select(u => u.Specjalizacja).Distinct().ToList();
                }
                return View(new List<Wizyta>());
            }

            if (!czyFormularzWyslany)
            {
                dataOd = DateTime.Today;
            }

            var query = _context.Wizyty
                .Include(w => w.Pacjent)
                .Include(w => w.Lekarz)
                .Include(w => w.Gabinet)
                .AsNoTracking();

            if (czyLekarz)
            {
                query = query.Where(w => w.LekarzID == zalogowanyId);
            }

            if (!string.IsNullOrWhiteSpace(szukajPacjenta))
            {
                string fraza = szukajPacjenta.ToLower();
                query = query.Where(w =>
                    w.Pacjent!.Pesel.Contains(fraza) ||
                    (w.Pacjent.Imie + " " + w.Pacjent.Nazwisko).ToLower().Contains(fraza)
                );
            }

            if (!czyLekarz && szukajLekarza.HasValue)
            {
                query = query.Where(w => w.LekarzID == szukajLekarza.Value);
            }

            if (!czyLekarz && !string.IsNullOrWhiteSpace(szukajSpecjalizacja))
            {
                query = query.Where(w => w.Lekarz!.Specjalizacja == szukajSpecjalizacja);
            }

            if (dataOd.HasValue)
            {
                query = query.Where(w => w.DataRozpoczecia >= dataOd.Value);
            }

            if (dataDo.HasValue)
            {
                DateTime koniecDnia = dataDo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(w => w.DataRozpoczecia <= koniecDnia);
            }

            var wizyty = query.OrderBy(w => w.DataRozpoczecia).ToList();

            if (!wizyty.Any() && string.IsNullOrEmpty(ViewBag.Komunikat))
            {
                ViewBag.Komunikat = "Nie znaleziono wizyt spełniających kryteria.";
            }

            if (!czyLekarz)
            {
                ViewBag.WszyscyLekarze = _context.Uzytkownicy.Where(u => (u.Permisje & 2) != 0 && u.CzyAktywny && !string.IsNullOrEmpty(u.Specjalizacja)).ToList();
                ViewBag.WszystkieSpecjalizacje = _context.Uzytkownicy
                    .Where(u => !string.IsNullOrEmpty(u.Specjalizacja) && u.CzyAktywny)
                    .Select(u => u.Specjalizacja)
                    .Distinct()
                    .ToList();
            }

            // --- TUTAJ JEST POPRAWIONY FORMAT Z GODZINĄ ---
            ViewBag.AktualnyPacjent = szukajPacjenta;
            ViewBag.AktualnyLekarz = szukajLekarza;
            ViewBag.AktualnaSpecjalizacja = szukajSpecjalizacja;
            ViewBag.AktualnaDataOd = dataOd?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.AktualnaDataDo = dataDo?.ToString("yyyy-MM-ddTHH:mm");

            return View(wizyty);
        }


        [HttpGet]
        public IActionResult UzupelnijWyniki(int id)
        {

            ModelState.AddModelError("", "Opis dolegliwości oraz zlecenia nie mogą pozostać puste");

            var idCookie = Request.Cookies["AuthUserId"];
            if (string.IsNullOrEmpty(idCookie) || !int.TryParse(idCookie, out int zalogowanyId))
            {
                return RedirectToAction("Login", "Account");
            }

            var wizyta = _context.Wizyty
                .Include(w => w.Pacjent)
                .Include(w => w.Lekarz)
                .Include(w => w.Gabinet)
                .FirstOrDefault(w => w.ID == id);

            if (wizyta == null) return NotFound();

            if (wizyta.LekarzID != zalogowanyId)
            {
                return Forbid();
            }

            if (wizyta.Status == "Zrealizowana")
            {
                ViewBag.TrybPodgladu = true;
                return View(wizyta);
            }

            if (wizyta.DataRozpoczecia > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Wizyta jeszcze się nie odbyła.";
                return RedirectToAction("ListaWizyt");
            }

            ViewBag.TrybPodgladu = false;
            return View(wizyta);
        }

  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UzupelnijWyniki(int id, string opisDoleglywosci, string zalecenia, string przepisaneLeki)
        {
            var idCookie = Request.Cookies["AuthUserId"];
            if (string.IsNullOrEmpty(idCookie) || !int.TryParse(idCookie, out int zalogowanyId))
            {
                return RedirectToAction("Login", "Account");
            }

            var wizyta = _context.Wizyty
                .Include(w => w.Pacjent)
                .Include(w => w.Lekarz)
                .Include(w => w.Gabinet)
                .FirstOrDefault(w => w.ID == id);

            if (wizyta == null) return NotFound();
            if (wizyta.LekarzID != zalogowanyId) return Forbid();

            
            if (string.IsNullOrWhiteSpace(opisDoleglywosci) || string.IsNullOrWhiteSpace(zalecenia))
            {
                ModelState.AddModelError("", "Opis dolegliwości oraz zalecenia nie mogą pozostać puste.");
                ViewBag.TrybPodgladu = false;

                wizyta.OpisDoleglywosci = opisDoleglywosci;
                wizyta.Zalecenia = zalecenia;
                wizyta.PrzepisaneLeki = przepisaneLeki;
                return View(wizyta);
            }

            wizyta.OpisDoleglywosci = opisDoleglywosci;
            wizyta.Zalecenia = zalecenia;
            wizyta.PrzepisaneLeki = przepisaneLeki;
            wizyta.Status = "Zrealizowana"; 

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Pomyślnie zapisano wyniki wizyty medycznej.";
            return RedirectToAction("ListaWizyt");
        }

        private void PrzygotujDaneDoFormularza()
        {
            ViewBag.Pacjenci = _context.Pacjenci.ToList();
            ViewBag.Lekarze = _context.Uzytkownicy.Where(u => (u.Permisje & 2) != 0 && u.CzyAktywny && !string.IsNullOrEmpty(u.Specjalizacja)).ToList();
            ViewBag.Gabinety = _context.Gabinety.ToList();
        }
    }
}