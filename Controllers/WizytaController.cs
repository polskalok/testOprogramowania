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

        // =========================================================================
        // --- PUNKT 22: REJESTRACJA WIZYTY (GET) ---
        // =========================================================================
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

        // =========================================================================
        // --- PUNKT 22: REJESTRACJA WIZYTY (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rejestruj(Wizyta model)
        {
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
                ModelState.AddModelError("DataRozpoczecia", "Brak wolnych terminów w tym okresie dla wybranego lekarza lub gabinetu.");
                PrzygotujDaneDoFormularza();
                return View(model);
            }

            model.Status = "Zarejestrowana";
            _context.Wizyty.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Pomyślnie zarejestrowano wizytę";
            return RedirectToAction("Rejestruj");
        }

        // =========================================================================
        // --- PUNKT 23 & 24: PRZEGLĄD I WYSZUKIWANIE WIZYT ---
        // =========================================================================
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

            var query = _context.Wizyty
                .Include(w => w.Pacjent)
                .Include(w => w.Lekarz)
                .Include(w => w.Gabinet)
                .AsNoTracking();

            if (czyLekarz)
            {
                query = query.Where(w => w.LekarzID == zalogowanyId);
            }

            if (!dataOd.HasValue && !dataDo.HasValue && string.IsNullOrEmpty(szukajPacjenta) && !szukajLekarza.HasValue && string.IsNullOrEmpty(szukajSpecjalizacja))
            {
                DateTime dzis = DateTime.Today;
                query = query.Where(w => w.DataRozpoczecia >= dzis);
            }

            if (!string.IsNullOrEmpty(szukajPacjenta))
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

            if (!czyLekarz && !string.IsNullOrEmpty(szukajSpecjalizacja))
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

            if (!wizyty.Any())
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

            ViewBag.AktualnyPacjent = szukajPacjenta;
            ViewBag.AktualnyLekarz = szukajLekarza;
            ViewBag.AktualnaSpecjalizacja = szukajSpecjalizacja;
            ViewBag.AktualnaDataOd = dataOd?.ToString("yyyy-MM-dd");
            ViewBag.AktualnaDataDo = dataDo?.ToString("yyyy-MM-dd");

            return View(wizyty);
        }

        // =========================================================================
        // --- PUNKT 25: REJESTRACJA WYNIKÓW WIZYTY (GET) ---
        // =========================================================================
        [HttpGet]
        public IActionResult UzupelnijWyniki(int id)
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

        // =========================================================================
        // --- PUNKT 25: REJESTRACJA WYNIKÓW WIZYTY (POST) ---
        // =========================================================================
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

            // 5a. Walidacja wyjątków: Opis dolegliwości oraz zalecenia nie mogą pozostać puste
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
            wizyta.Status = "Zrealizowana"; // Nieodwracalna zmiana statusu

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