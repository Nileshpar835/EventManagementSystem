using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;
using System.Linq;

namespace EventManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly VenueService _venueService;

    public HomeController( VenueService venueService)
    {
        _venueService = venueService;
    }

    public IActionResult Index()
    {
        var venues = _venueService.GetAllVenues();
        ViewBag.Locations = venues.Select(v => v.Location).Distinct().OrderBy(l => l).ToList();
        ViewBag.LatestReviews = _venueService.GetLatestReviews();
        return View(venues);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
