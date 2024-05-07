using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<FlightTracker>(f=>new FlightTracker(GetWordDictionary(),f.GetRequiredService<IMemoryCache>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

ConcurrentDictionary<string,double[]> GetWordDictionary()
{
	
	var jsonArray = File.ReadAllText("./airaports.json");
	return JsonSerializer.Deserialize<ConcurrentDictionary<string,double[]>>(jsonArray);
}
