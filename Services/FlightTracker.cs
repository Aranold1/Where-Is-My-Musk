using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic;
using MuskMotions.Models;

public class FlightTracker
{
	private readonly ConcurrentDictionary<string, string[]> _wordTagDictionary;

	private readonly Dictionary<string, Airplane> _airplanesFromLastSuccesfulResponse;

	public FlightTracker(ConcurrentDictionary<string, string[]> wordTagDictionary)
	{
		_wordTagDictionary = wordTagDictionary;
		var path = "./data/airplanes.json";
		if (File.Exists(path))
		{
			_airplanesFromLastSuccesfulResponse = JsonSerializer.Deserialize<Dictionary<string, Airplane>>(File.ReadAllText(path));
		}
		else
		{
			_airplanesFromLastSuccesfulResponse = new Dictionary<string, Airplane>();
		}

	}
	//source https://grndcntrl.net/falconlanding/
	private readonly string[] icaoCodes = new string[]
	{
		"a835af", // 2015 Gulfstream G650ER
		"a2ae0a", // 2007 Gulfstream G550
		"a64304", // 2004 Gulfstream G550
		"a0dac5", // 2002 Boeing 737-800
		"a572b9"  // 2004 Gulfstream G450
	};

	public async ValueTask<List<Airplane>> GetAirplanesAsync()
	{
		var addAirplanesToListTasks = icaoCodes.Select(GetAriplanesFromApiAsync).ToList();

		var results = await Task.WhenAll(addAirplanesToListTasks);
		var successfulResults = results.Where(x => x != null).ToList();

		if (successfulResults.Count > 0)
		{
			foreach (var plane in successfulResults)
			{
				if (_airplanesFromLastSuccesfulResponse.Keys.Contains(plane.Icao))
				{
					_airplanesFromLastSuccesfulResponse[plane.Icao] = plane;
				}
				else
				{
					_airplanesFromLastSuccesfulResponse.Add(plane.Icao, plane);
				}
			}
			File.WriteAllText("./data/airplanes.json", JsonSerializer.Serialize(_airplanesFromLastSuccesfulResponse));
			return _airplanesFromLastSuccesfulResponse.Values.ToList();
		}
		else
		{
			return _airplanesFromLastSuccesfulResponse.Values.ToList();
		}

	}

	private async Task<Airplane> GetAriplanesFromApiAsync(string icao)
	{
		var unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var twentyNineDaysInUnixTime = 2505600;

		using (var httpClient = new HttpClient())
		{
			try
			{
				var response = await httpClient.GetAsync($"https://opensky-network.org/api/flights/aircraft?icao24={icao}&begin={unixTimeNow - twentyNineDaysInUnixTime}&end={unixTimeNow}");

				response.EnsureSuccessStatusCode();
				var jsonArray = JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray();
				var airportTagsForLast30Days = jsonArray.Where(n => n["estArrivalAirport"] != null).Select(n => n["estArrivalAirport"].ToString());
				var lastSeensFor30DaysInUnixTime = jsonArray.Where(n => n["lastSeen"] != null).Select(n => int.Parse(n["lastSeen"].ToString()));
				var plane = new Airplane
				{
					Icao = icao,
					Latitude = _wordTagDictionary[airportTagsForLast30Days.First()][0],
					Longitude = _wordTagDictionary[airportTagsForLast30Days.First()][1],
					LastSeensForLast30Days = lastSeensFor30DaysInUnixTime.Select(x => DateTimeOffset.FromUnixTimeSeconds(x).DateTime).ToList(),
					CoordinatesForLast30Days = airportTagsForLast30Days.Select(x => new string[] { _wordTagDictionary[x][0], _wordTagDictionary[x][1] }).ToList()
				};
				return plane;
			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex.Message);
				return null;
			}

		}

	}
}
