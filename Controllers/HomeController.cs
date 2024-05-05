using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace MuskMotions.Controllers;

public class HomeController : Controller
{
	FlightTracker _flightTracker;
	public HomeController(FlightTracker flightTracker)
	{
		_flightTracker = flightTracker;
	}
	[Route("/")]
	public async Task<IActionResult> Home()
	{
		var planes = await _flightTracker.GetAirplanesAsync();
		foreach (var item in planes)
		{
			System.Console.WriteLine(item.Latitude);
			System.Console.WriteLine(item.Longitude);
		}
		return View();
	}
}
