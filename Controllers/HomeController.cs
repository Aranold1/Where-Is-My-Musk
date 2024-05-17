using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MuskMotions.Models;


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
		var sw = new Stopwatch();
		sw.Start();
		var airplanes = await _flightTracker.GetAirplanesAsync();
		sw.Stop();
		System.Console.WriteLine(sw);
		return View(airplanes);
	}
	[Route("/support")]
	public async Task<IActionResult> Support()
	{
		return View();
	}
}
