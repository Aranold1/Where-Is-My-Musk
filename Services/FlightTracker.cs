using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic;
using MuskMotions.Models;
public class FlightTracker
{
	readonly ConcurrentDictionary<string,string[]> _wordTagDictionary;
	readonly IMemoryCache _imemoryCahce;
	List<Airplane> airplanes;

	public FlightTracker(ConcurrentDictionary<string,string[]> wordTagDictionary,IMemoryCache cache) 
	{
		_imemoryCahce = cache;
		_wordTagDictionary = wordTagDictionary;
	}


	public async ValueTask<List<Airplane>> GetAirplanesAsync()
	{
		if (_imemoryCahce.TryGetValue("airplanes",out List<Airplane> airplanesCache))
		{
			return airplanesCache;
		}
		airplanes = new List<Airplane>();

		string[] icaoCodes = new string[]
		{
			"a835af", // 2015 Gulfstream G650ER
			"a2ae0a", // 2007 Gulfstream G550
			"a64304", // 2004 Gulfstream G550
			"A0DAC5", // 2002 Boeing 737-800
			"A572B9"  // 2004 Gulfstream G450
		};
		var addAirplanesToListTasks = new List<Task>();
		for (int i = 0; i < icaoCodes.Length; i++)
		{
			var addAirplanesToListTask = AddAirPlanesToListAsync(icaoCodes[i]);
			addAirplanesToListTasks.Add(addAirplanesToListTask);
		}
		await Task.WhenAll(addAirplanesToListTasks);
		_imemoryCahce.Set("airplanes",airplanes,TimeSpan.FromMinutes(10));
		return airplanes;
	}

	async Task AddAirPlanesToListAsync(string icao)
	{
		var unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		//api allows take only 30 days interval 
		var twentyNineDaysUnixTime = 2505600;
		try
		{
			using (var httpClient = new HttpClient())
			{
				var response = await httpClient.
				GetAsync($"https://opensky-network.org/api/flights/aircraft?icao24={icao.ToLower()}&begin={unixTimeNow-twentyNineDaysUnixTime}&end={unixTimeNow}");
				response.EnsureSuccessStatusCode();
				var jsonArray = JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray();
				var airportTagsForLast30Days = jsonArray.Where(n=>n["estArrivalAirport"]!=null).Select(n=>n["estArrivalAirport"].ToString());
				var numbers = new char[]{'0','1','2','3','4','5','6','7','8','9'};
				var plane = new Airplane
				{
					Icao = icao,
					Latitude = _wordTagDictionary[airportTagsForLast30Days.Last()][0],
					Longitude = _wordTagDictionary[airportTagsForLast30Days.Last()][1]
				};
				lock (airplanes)
				{
					airplanes.Add(plane);
				}
			}
		}
		catch
			{

		}
	}
}