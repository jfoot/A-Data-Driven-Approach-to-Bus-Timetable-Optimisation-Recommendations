// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;

namespace ReadingBusesAPI.BusServices
{
	/// <summary>
	///     This classes simply gets all the bus services operated by Reading Buses, by interfacing with the "List Of Services"
	///     API.
	/// </summary>
	internal class Services
	{
		/// <value>the location for the service cache file.</value>
		private const string CacheLocation = "cache\\Services.cache";

		/// <summary>
		///     Finds all the services operated by Reading Buses.
		/// </summary>
		/// <exception cref="ReadingBusesApiException">Thrown if an invalid or expired API Key is used.</exception>
		internal async Task<List<BusService>> FindServices()
		{
			if (!File.Exists(CacheLocation) || !ReadingBuses.Cache)
			{
				string json = await
					new WebClient().DownloadStringTaskAsync(
						UrlConstructor.ListOfServices()).ConfigureAwait(false);

				List<BusService> newServicesData = new List<BusService>();

				try
				{
					newServicesData = JsonConvert.DeserializeObject<List<BusService>>(json)
						.OrderBy(p => Convert.ToInt32(Regex.Replace(p.ServiceId, "[^0-9.]", ""))).ToList();

					// Save the JSON file for later use. 
					if (ReadingBuses.Cache)
					{
						File.WriteAllText(CacheLocation,
							JsonConvert.SerializeObject(newServicesData, Formatting.Indented));
					}
				}
				catch (JsonSerializationException)
				{
					ErrorManager.TryErrorMessageRetrieval(json);
				}


				return newServicesData;
			}
			else
			{
				DirectoryInfo ch = new DirectoryInfo(CacheLocation);
				//if ((DateTime.Now - ch.CreationTime).TotalDays > ReadingBuses.CacheValidityLength)
				//{
				//	File.Delete(CacheLocation);
				//	ReadingBuses.PrintWarning("Warning: Cache data expired, downloading latest Services Data.");
				//	return await FindServices().ConfigureAwait(false);
				//}

				try
				{
					return JsonConvert.DeserializeObject<List<BusService>>(
						File.ReadAllText(CacheLocation));
				}
				catch (JsonSerializationException)
				{
					File.Delete(CacheLocation);
					ReadingBuses.PrintWarning(
						"Warning: Unable to read Services Cache File, deleting and regenerating cache.");
					return await FindServices().ConfigureAwait(false);
				}
			}
		}
	}
}
