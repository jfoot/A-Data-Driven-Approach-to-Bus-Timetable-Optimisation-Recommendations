// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;

namespace ReadingBusesAPI.BusStops
{
	/// <summary>
	///     This classes simply gets all the buses stops visited by Reading Buses, by interfacing with the "List Of Bus Stops"
	///     API.
	/// </summary>
	internal class Locations
	{
		/// <value>the location for the service cache file.</value>
		private const string CacheLocation = "cache\\Locations.cache";


		/// <summary>
		///     Finds all the bus stops visited by Reading Buses.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if an invalid or expired API Key is used.</exception>
		internal async Task<ConcurrentDictionary<string, BusStop>> FindLocations()
		{
			if (!File.Exists(CacheLocation) || !ReadingBuses.Cache)
			{
				string json = await new WebClient().DownloadStringTaskAsync(UrlConstructor.ListOfBusStops())
					.ConfigureAwait(false);
				var locationsFiltered = new ConcurrentDictionary<string, BusStop>();

				try
				{
					List<BusStop> locations = JsonConvert.DeserializeObject<List<BusStop>>(json);

					foreach (var location in locations)
					{
						if (!locationsFiltered.ContainsKey(location.AtcoCode))
						{
							locationsFiltered.TryAdd(location.AtcoCode, location);
						}
					}

					if (ReadingBuses.Cache)
					{
						File.WriteAllText(CacheLocation,
							JsonConvert.SerializeObject(locationsFiltered,
								Formatting.Indented)); // Save the JSON file for later use.  
					}
				}
				catch (JsonSerializationException)
				{
					ErrorManager.TryErrorMessageRetrieval(json);
				}

				return locationsFiltered;
			}
			else
			{
				//DirectoryInfo ch = new DirectoryInfo(CacheLocation);
				//if ((DateTime.Now - ch.CreationTime).TotalDays > ReadingBuses.CacheValidityLength)
				//{
				//	File.Delete(CacheLocation);
				//	ReadingBuses.PrintWarning("Warning: Cache data expired, downloading latest Locations Data.");
				//	return await FindLocations().ConfigureAwait(false);
				//}


				try
				{
					return JsonConvert.DeserializeObject<ConcurrentDictionary<string, BusStop>>(
						File.ReadAllText(CacheLocation));
				}
				catch (JsonSerializationException)
				{
					File.Delete(CacheLocation);
					ReadingBuses.PrintWarning(
						"Warning: Unable to read Locations Cache File, deleting and regenerating cache.");
					return await FindLocations().ConfigureAwait(false);
				}
			}
		}
	}
}
