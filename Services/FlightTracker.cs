
using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using MuskMotions.Models;
public class FlightTracker
{
	readonly ConcurrentDictionary<string,string[]> _wordTagDictionary;
	readonly IMemoryCache _imemoryCahce;
	
	readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

	public FlightTracker(ConcurrentDictionary<string,string[]> wordTagDictionary,IMemoryCache cache) 
	{
		_imemoryCahce = cache;
		_wordTagDictionary = wordTagDictionary;
	}

	public async ValueTask<List<Airplane>> GetAirplanesAsync()
	{
		if (_imemoryCahce.TryGetValue("airplanes",out List<Airplane> airplanes))
		{
			return airplanes;
		}
		string[] icaoCodes = new string[]
		{
			"a835af", // 2015 Gulfstream G650ER
			"a2ae0a", // 2007 Gulfstream G550
			"a64304", // 2004 Gulfstream G550
			"A0DAC5", // 2002 Boeing 737-800
			"A572B9"  // 2004 Gulfstream G450
		};
		airplanes = new List<Airplane>();
		for (int i = 0; i < icaoCodes.Length; i++)
		{
			try
			{
				var currnetCoordinates = await GetPlaneCoordinatesAsync(icaoCodes[i]);
				var airPlane = new Airplane()
				{
					Icao = icaoCodes[i],
					Latitude = currnetCoordinates[0],
					Longitude = currnetCoordinates[1],
					//at this point all plane gonna have the same picture
					Picture = ""
				};
				airplanes.Add(airPlane);
			}
			catch
			{
			}
		}
		_imemoryCahce.Set("airplanes",airplanes,TimeSpan.FromHours(2));
		return airplanes;
	}
	async Task<string[]> GetPlaneCoordinatesAsync(string Icao)
	{
		var openSkyClient = new HttpClient();
	

		var unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		//api allows take only 30 days interval 
		var twentyNineDaysUnixTime = 2505600;
		
		try
		{	
			var lastFlightsFor30DaysJson = await openSkyClient.
		GetAsync($"https://opensky-network.org/api/flights/aircraft?icao24={Icao.ToLower()}&begin={unixTimeNow-twentyNineDaysUnixTime}&end={unixTimeNow}");
			
			lastFlightsFor30DaysJson.EnsureSuccessStatusCode();
			var jsonArray = JsonNode.Parse(await @lastFlightsFor30DaysJson.Content.ReadAsStringAsync()).AsArray();
		
			var airportTagsForLast30Days = jsonArray.Where(n=>n["estArrivalAirport"]!=null).Select(n=>n["estArrivalAirport"].ToString());
			var numbers = new char[]{'0','1','2','3','4','5','6','7','8','9'};

			return _wordTagDictionary[airportTagsForLast30Days.Last()];
		}
		catch
		{
			throw new Exception();
		}
	}
}