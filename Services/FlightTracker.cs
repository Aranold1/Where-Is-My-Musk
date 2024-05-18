using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MuskMotions.Models;
using System.Collections.Generic;
using System.Linq;
using System;

public class FlightTracker
{
    private readonly ConcurrentDictionary<string, string[]> _wordTagDictionary;
    private readonly IMemoryCache _memoryCache;
    private List<Airplane> airplanes;
    private Timer _refreshTimer;

    public FlightTracker(ConcurrentDictionary<string, string[]> wordTagDictionary, IMemoryCache cache)
    {
        _memoryCache = cache;
        _wordTagDictionary = wordTagDictionary;
        StartCacheRefreshTimer();
    }

    private readonly string[] icaoCodes = new string[]
    {
        "a835af", // 2015 Gulfstream G650ER
        "a2ae0a", // 2007 Gulfstream G550
        "a64304", // 2004 Gulfstream G550
        "A0DAC5", // 2002 Boeing 737-800
        "A572B9"  // 2004 Gulfstream G450
    };

    public async ValueTask<List<Airplane>> GetAirplanesAsync()
    {
        if (_memoryCache.TryGetValue("airplanes", out List<Airplane> airplanesCache))
        {
            return airplanesCache;
        }

        airplanes = new List<Airplane>();
        var addAirplanesToListTasks = icaoCodes.Select(AddAirPlanesToListAsync).ToList();

        await Task.WhenAll(addAirplanesToListTasks);
        _memoryCache.Set("airplanes", airplanes, TimeSpan.FromMinutes(5.2));

        return airplanes;
    }

    private async Task AddAirPlanesToListAsync(string icao)
    {
        var unixTimeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var twentyNineDaysUnixTime = 2505600;
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"https://opensky-network.org/api/flights/aircraft?icao24={icao.ToLower()}&begin={unixTimeNow - twentyNineDaysUnixTime}&end={unixTimeNow}");
                response.EnsureSuccessStatusCode();
                var jsonArray = JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray();
                var airportTagsForLast30Days = jsonArray.Where(n => n["estArrivalAirport"] != null).Select(n => n["estArrivalAirport"].ToString());
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching airplane data for {icao}: {ex.Message}");
        }
    }

    private void StartCacheRefreshTimer()
    {
        // Set timer to refresh cache 40 seconds (20 seconds buffer + 20 seconds before expiration) before cache expiration
        var dueTime = TimeSpan.FromMinutes(5);
        var period = TimeSpan.FromMinutes(1);

        _refreshTimer = new Timer(async _ => await RefreshCache(), null, dueTime, period);
    }

    private async Task RefreshCache()
    {
        try
        {
            var airplanesList = await GetAirplanesAsync();
            _memoryCache.Set("airplanes", airplanesList, TimeSpan.FromMinutes(1));
            Console.WriteLine("Cache refreshed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing cache: {ex.Message}");
        }
    }
}
