using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;
using System.Diagnostics;
using System.Linq;

namespace EventManagementSystem.Controllers
{
    public class VenuesController : Controller
    {
        private readonly VenueService _venueService;

        public VenuesController( VenueService venueService)
        {
            _venueService = venueService;
        }

        public IActionResult Index()
        {
            var venues = _venueService.GetAllVenues();
            ViewBag.Locations = venues.Select(v => v.Location).Distinct().OrderBy(l => l).ToList();
            return View(venues);
        }

        public IActionResult Details(int id)
        {
            var venue = _venueService.GetVenueById(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        public IActionResult Search(string query, string location, int? capacity, string venueType, string priceRange)
        {
            var allVenues = _venueService.GetAllVenues();
            ViewBag.Locations = allVenues.Select(v => v.Location).Distinct().OrderBy(l => l).ToList();
            
            var venues = _venueService.SearchVenues(query, location, capacity, venueType, priceRange);
            return View("Index", venues);
        }

        public IActionResult Browse()
        {
            var venues = _venueService.GetAllVenues();
            ViewBag.Locations = venues.Select(v => v.Location).Distinct().OrderBy(l => l).ToList();
            return View(venues);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 