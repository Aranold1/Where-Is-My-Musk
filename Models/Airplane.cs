using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MuskMotions.Models
{
	public class Airplane
	{
		public string Icao { get; set; }
		public string Latitude { get; set; }
		public string Longitude { get; set; }
		public DateTime LastSeen { get; set; }

		public List<DateTime> LastSeensForLast30Days { get; set; }
		public List<string[]> CoordinatesForLast30Days { get; set; }
		public Airplane()
		{
			LastSeen = LastSeensForLast30Days[0];
		}
	}
}