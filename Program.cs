using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<FlightTracker>(f=>new FlightTracker(GetWordDictionary(),GetWordNumericTagDictionary(),f.GetRequiredService<IMemoryCache>()));

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


ConcurrentDictionary<string,double[]> GetWordNumericTagDictionary()
{
	var dict = new ConcurrentDictionary<string,double[]>();

	var path = "./airaports.txt";

	var content = File.ReadLines(path);

	foreach (var item in content)
	{
		var itemClear = item.Replace("{", "").Replace("}", "");
		string[] parts = itemClear.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length==3)
		{
			var coordinates = new double[]
			{
				double.Parse(parts[1],NumberStyles.Any, CultureInfo.InvariantCulture),
				double.Parse(parts[2],NumberStyles.Any, CultureInfo.InvariantCulture)
			};
			dict.TryAdd(parts[0],coordinates);
		}
	}
	return dict;
}
//far from ideal but i need to move far 
ConcurrentDictionary<string,double[]> GetWordDictionary()
{
	var dict = new ConcurrentDictionary<string,double[]>();
	var path =  "./iata-icao.csv";
	using (var reader = new StreamReader(path))
	{
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			var data = new string(line.ToCharArray().Where(x=>x!='"').ToArray()).Split(',');
			
			try
			{
				if (!string.IsNullOrEmpty(data[3]))
				{
					var coordinates = new double[]
					{
						double.Parse(data[5],NumberStyles.Any, CultureInfo.InvariantCulture),
						double.Parse(data[6],NumberStyles.Any, CultureInfo.InvariantCulture)
					};
					dict.TryAdd(data[3].Trim(),coordinates);
				}
			}
			catch
			{
				try
				{
					var tag = data.First(s=>s.Length==4);
					var coordinates = new double[]
					{
						double.Parse(data[data.Length-2],NumberStyles.Any, CultureInfo.InvariantCulture),
						double.Parse(data[data.Length-1],NumberStyles.Any, CultureInfo.InvariantCulture)
					};
					dict.TryAdd(tag,coordinates);
				}
				catch
				{

				}
			}
		}
		return dict;
	}
}
