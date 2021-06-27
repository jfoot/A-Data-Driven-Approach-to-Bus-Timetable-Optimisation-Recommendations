// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.TimeTable;
using ReadingBusesAPI.VehiclePositions;

namespace ReadingBusesAPI
{
	/// <summary>
	///     This is the main class for the library, here you can initialise a singleton instance and then query and use the
	///     Reading Buses API.
	/// </summary>
	/// <example>
	///     <code>
	/// //Optional
	/// ReadingBuses.SetCache(true);
	/// ReadingBuses Controller =  await ReadingBuses.Initialise("API KEY HERE");
	/// 
	/// BusService service = Controller.GetService("17"); or ReadingBuses.GetInstance().GetService("17");
	/// </code>
	/// </example>
	/// <remarks>
	///     Cached Data is data stored locally in JSON and XML files, stored in a hidden folder called "cache", in the same
	///     directory the program is executed from.
	///     This is a copy of the results from an API call, such as the bus services and bus stops, because it is unlikely for
	///     this data to change regularly.
	///     By default the cached data will be updated every 7 days, but you can request new data or disable cache if you wish.
	///     Caching data is however faster
	///     as you do not need to keep making API requests for data likely to be the same.
	/// </remarks>
	public sealed class ReadingBuses
	{
		/// <value>The singleton instance</value>
		private static ReadingBuses _instance;

		/// <value>Holds information on all the bus stops/locations visited by Reading Buses</value>
		private ConcurrentDictionary<string, BusStop> _locations;

		/// <value>Holds information on all the services operated by Reading Buses</value>
		private List<BusService> _services;


		/// <summary>
		///     Create a new Reading Buses library object, this is the main control.
		/// </summary>
		/// <param name="apiKey">The Reading Buses API Key, get your own from http://rtl2.ods-live.co.uk/cms/apiservice </param>
		private ReadingBuses(string apiKey)
		{
			ApiKey = apiKey;
			GpsController = new GpsController();
		}

		/// <value>Keeps track of if cache data is being used or not</value>
		internal static bool Debugging { get; private set; }

		/// <value>Keeps track of if cache data is being used or not</value>
		internal static bool Cache { get; private set; } = true;

		/// <value>Keeps track of if warnings are being outputted to console or not.</value>
		internal static bool Warning { get; private set; } = true;

		/// <value>Keeps track of if full error logs are being outputted to console or not.</value>
		internal static bool FullError { get; private set; }

		/// <value>Stores how many days cache data is valid for in days before being regenerated</value>
		internal static int CacheValidityLength { get; private set; } = 7;

		/// <value>Holds the users API Key.</value>
		internal static string ApiKey { get; private set; }

		/// <value>Stores the GPS controller, which can help get vehicle GPS data.</value>
		public GpsController GpsController { get; }

		/// <summary>
		///     Creates cache data and retrieves bus services and bus stop data.
		/// </summary>
		/// <returns>Gets the basic bus stops and services data.</returns>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">If the API key is invalid or expired.</exception>
		private async Task SetUp()
		{
			try
			{
				if (!Directory.Exists("cache"))
				{
					Directory.CreateDirectory("cache");
					DirectoryInfo ch = new DirectoryInfo("cache") {Attributes = FileAttributes.Hidden};
				}

				Task<List<BusService>> servicesTask = new Services().FindServices();
				Task<ConcurrentDictionary<string, BusStop>> locationsTask = new Locations().FindLocations();


				await Task.WhenAll(servicesTask, locationsTask).ConfigureAwait(false);
				_services = await servicesTask;
				_locations = await locationsTask;
			}
			catch (AggregateException ex)
			{
				DeleteInstance();
				PrintFullErrorLogs(ex.Message);
				throw new ReadingBusesApiExceptionBadQuery(ex.InnerExceptions[0].Message);
			}
		}

		/// <summary>
		///     Deletes the current singleton instance if there was an error generating one.
		/// </summary>
		private static void DeleteInstance()
		{
			_instance = null;
		}

		/// <summary>
		///     Sets if you want to cache data into local files or always get new data from the API, which will take longer.
		/// </summary>
		/// <param name="value">True or False for if you want to get Cache or live data.</param>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if you attempt to change the cache options after the library has
		///     been instantiated
		/// </exception>
		public static void SetCache(bool value)
		{
			if (_instance == null)
			{
				Cache = value;
			}
			else
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"Cache Storage Setting can not be changed once ReadingBuses Object is initialized.");
			}
		}


		/// <summary>
		///     Sets if you want to debug the library by making requests to a dummy server instead of the real live sever.
		/// </summary>
		/// <param name="value">True or False for if you want to debug or not.</param>
		/// <remarks>Unless you are developing or editing library in some way you should not need to use this.</remarks>
		public static async Task SetDebuggingAsync(bool value)
		{
			if (Debugging != value)
			{
				Debugging = value;
                await _instance?.SetUp();
            }
		}


		/// <summary>
		///     Sets if you want to print out warning messages to the console screen or not. Only done so in debug.
		/// </summary>
		/// <param name="value">True or False for printing warning messages.</param>
		public static void SetWarning(bool value) => Warning = value;

		/// <summary>
		///     Sets if you want to print out the full error logs to console, only needed for debugging library errors. Only done
		///     so in debug.
		/// </summary>
		/// <param name="value">True or False for printing full error logs to console.</param>
		public static void SetFullError(bool value) => FullError = value;

		/// <summary>
		///     Sets how long to keep Cache data for before invalidating it and getting new data.
		/// </summary>
		/// <param name="days">The number of days to store the cache data for before getting new data.</param>
		public static void SetCacheValidityLength(int days) => CacheValidityLength = days;

		/// <summary>
		///     Deletes any Cache data stored, Cache data is deleted automatically after a number of days, use this only if you
		///     need to force new data early.
		/// </summary>
		public static void InvalidateCache() => Directory.Delete("cache", true);

		/// <summary>
		///     Internal method for printing warning messages to the console screen. Only done so in debug.
		/// </summary>
		/// <param name="message">The message to print off to console.</param>
		[Conditional("DEBUG")]
		internal static void PrintWarning(string message)
		{
			if (Warning)
			{
				Console.WriteLine(message);
			}
		}

		/// <summary>
		///     Internal method for printing warning messages to the console screen. Only done so in debug.
		/// </summary>
		/// <param name="message">The message to print off to console.</param>
		[Conditional("DEBUG")]
		internal static void PrintFullErrorLogs(string message)
		{
			if (FullError)
			{
				Console.WriteLine(message);
			}
		}


		/// <summary>
		///     Used to initially initialise the ReadingBuses Object, it is recommended you do this in your programs start up.
		/// </summary>
		/// <param name="apiKey">The Reading Buses API Key, get your own from http://rtl2.ods-live.co.uk/cms/apiservice </param>
		/// <returns>An instance of the library controller. This same instance can be got by calling the "GetInstance" method.</returns>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Can throw an exception if you pass an invalid or expired API Key.</exception>
		/// See
		/// <see cref="ReadingBuses.GetInstance()" />
		/// to get any future instances afterwards.
		public static async Task<ReadingBuses> Initialise(string apiKey)
		{
			if(string.IsNullOrEmpty(apiKey))
				throw new InvalidOperationException(
				"You must provide an API Key to initialise this object.");

			if (_instance == null)
			{
				_instance = new ReadingBuses(apiKey);
				await _instance.SetUp().ConfigureAwait(false);
			}

			return _instance;
		}

		/// <summary>
		///     You will never need more than one ReadingBuses object, a singleton is used to ensure you always get the same
		///     instance.
		/// </summary>
		/// <returns>Returns the ReadingBuses object to be used throughout your program.</returns>
		/// <exception cref="InvalidOperationException">
		///     Thrown if you attempt to get an instance before you have called the
		///     "Initialise" function.
		/// </exception>
		/// See
		/// <see cref="ReadingBuses.Initialise(string)" />
		/// to initially initialise the ReadingBuses Object singleton.
		public static ReadingBuses GetInstance()
		{
			if (_instance == null)
			{
				throw new InvalidOperationException(
					"You must first initialise the object before usage, call the 'Initialise' function passing your API Key.");
			}

			return _instance;
		}


		#region Operators

		/// <summary>
		///     Converts the short hand code for an operator into its Enum value, for example RGB stands for Reading Buses.
		/// </summary>
		/// <param name="operatorCodeS"></param>
		/// <returns>The Enum equivalent of the bus operator short code.</returns>
		internal static Company GetOperatorE(string operatorCodeS)
		{
			if (operatorCodeS.Equals("RGB", StringComparison.OrdinalIgnoreCase))
			{
				return Company.ReadingBuses;
			}

			if (operatorCodeS.Equals("KC", StringComparison.OrdinalIgnoreCase))
			{
				return Company.Kennections;
			}

			if (operatorCodeS.Equals("N&D", StringComparison.OrdinalIgnoreCase))
			{
				return Company.NewburyAndDistrict;
			}

			return Company.Other;
		}

		#endregion

		#region Locations

		/// <summary>
		///     Get a bus stop location based upon a bus stops location code
		/// </summary>
		/// <param name="actoCode">The code of the bus stop</param>
		/// <returns>A Bus Stop object for the Acto Code specified.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if the bus stop does not exist. You should first check with
		///     'IsLocation' If there is any uncertainty.
		/// </exception>
		/// See
		/// <see cref="ReadingBuses.IsLocation(string)" />
		/// to check if it is a location.
		public BusStop GetLocation(string actoCode)
		{
			if (IsLocation(actoCode))
			{
				return _locations[actoCode];
			}

			throw new ReadingBusesApiExceptionMalformedQuery(
				"A bus stop of that Acto Code can not be found, please make sure you have a valid Bus Stop Code. You can use, the 'IsLocation' function to check beforehand.");
		}

		/// <summary>
		///     All the bus stop locations that Reading Buses Visits
		/// </summary>
		/// <returns>All the bus stops Reading Buses visits</returns>
#pragma warning disable CA1822 // Mark members as static
		public BusStop[] GetLocations() => _locations.Values.ToArray();
#pragma warning restore CA1822 // Mark members as static

		/// <summary>
		///     Checks to see if the acto code for the bus stop exists in the API feed or not.
		/// </summary>
		/// <param name="actoCode">The ID Code for a bus stop.</param>
		/// <returns>True or False depending on if the stop is in the API feed or not.</returns>
#pragma warning disable CA1822 // Mark members as static
		public bool IsLocation(string actoCode) => _locations.ContainsKey(actoCode);
#pragma warning restore CA1822 // Mark members as static

		#endregion

		#region Services

		/// <summary>
		///     All the Services Reading Buses Operates
		/// </summary>
		/// <returns>All the Services Reading Buses Operates</returns>
		public BusService[] GetServices() => _services.ToArray();


		/// <summary>
		///     Returns all services Reading Buses Operates under a brand name, for example "pink" would return "22,25,27,29"
		///     services.
		/// </summary>
		/// <param name="brandName">The brand name for the services you wish to find, eg "pink" or "sky blue".</param>
		/// <returns>An array of Bus Services which are of the brand name specified.</returns>
		public BusService[] GetServices(string brandName) => _services
			.Where(o => string.Equals(o.BrandName, brandName, StringComparison.CurrentCultureIgnoreCase)).ToArray();


		/// <summary>
		///     Returns all services Reading Buses Operates under a specific operator.
		/// </summary>
		/// <param name="operatorCode">The operator/ company to filter by.</param>
		/// <returns>An array of Bus Services which are of the brand name specified.</returns>
		public BusService[] GetServices(Company operatorCode) => _services
			.Where(o => o.OperatorCode == operatorCode).ToArray();


		/// <summary>
		///     Returns a service which matches the Service Number passed,
		///     because the Reading Buses API now supports, Kennections and Newbury and District a service number can no longer be
		///     considered unique.
		/// </summary>
		/// <param name="serviceNumber">The service number/ID for the service you wish to be returned eg: 17 or 22.</param>
		/// <returns>The services matching the ID.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if the bus services does not exist. You should first check with
		///     'IsService' If there is any uncertainty.
		/// </exception>
		/// See
		/// <see cref="ReadingBuses.IsService(string)" />
		/// to check if it is a service.
		public BusService[] GetService(string serviceNumber)
		{
			if (IsService(serviceNumber))
			{
				return _services.Where(o =>
					string.Equals(o.ServiceId, serviceNumber, StringComparison.CurrentCultureIgnoreCase)).ToArray();
			}

			throw new ReadingBusesApiExceptionMalformedQuery(
				"The service number provided does not exist. You can check if it exists by calling 'IsService' first.");
		}


		/// <summary>
		///     Returns a service which matches the Service Number passed and the bus operator.
		/// </summary>
		/// <param name="serviceNumber">The service number/ID for the service you wish to be returned eg: 17 or 22.</param>
		/// <param name="operators">The bus operator to search in, for example "ReadingBuses"</param>
		/// <returns>The services matching the ID.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if the bus services does not exist. You should first check with
		///     'IsService' If there is any uncertainty.
		/// </exception>
		/// See
		/// <see cref="ReadingBuses.IsService(string)" />
		/// to check if it is a service.
		public BusService GetService(string serviceNumber, Company operators)
		{
			if (IsService(serviceNumber, operators))
			{
				return _services.Single(o =>
					string.Equals(o.ServiceId, serviceNumber, StringComparison.CurrentCultureIgnoreCase) &&
					o.OperatorCode.Equals(operators));
			}

			throw new ReadingBusesApiExceptionMalformedQuery(
				"The service number '"+ serviceNumber + "', provided does not exist. You can check if it exists by calling 'IsService' first.");
		}


		/// <summary>
		///     Checks to see if a service of that number exists or not in the API feed.
		/// </summary>
		/// <param name="serviceNumber">The service number to find.</param>
		/// <returns>True or False for if a service is the API feed or not.</returns>
		public bool IsService(string serviceNumber) => _services.Any(o =>
			string.Equals(o.ServiceId, serviceNumber, StringComparison.CurrentCultureIgnoreCase));


		/// <summary>
		///     Checks to see if a service of that number exists or not in the API feed, for a specific bus operator.
		/// </summary>
		/// <param name="serviceNumber">The service number to find.</param>
		/// <param name="operators">The specific bus operator you want to search in.</param>
		/// <returns>True or False for if a service is the API feed or not.</returns>
		public bool IsService(string serviceNumber, Company operators) => _services.Any(o =>
			string.Equals(o.ServiceId, serviceNumber, StringComparison.CurrentCultureIgnoreCase) &&
			o.OperatorCode.Equals(operators));


		/// <summary>
		///     Gets the archived real bus departure and arrival times along with their time table history for a specific vehicle,
		///     on a specific date.
		///     This can be used to find how late a vehicle was throughout that day.
		/// </summary>
		/// <param name="date">The date you want a report for, must be in the past.</param>
		/// <param name="vehicle">The vehicle ID number </param>
		/// <returns>An array of Archived Bus Departure and arrival times with their timetabled data.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have tried to get data for a date in the future. Or if you have not provided any date, and/or you have not
		///     provided at least either the service or location or vehicle.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
#pragma warning disable CA1822 // Mark members as static
		public Task<ArchivedBusTimeTable[]> GetVehicleTrackingHistory(DateTime date, string vehicle)
#pragma warning restore CA1822 // Mark members as static
		{
			return ArchivedBusTimeTable.GetTimeTable(null, date, null, vehicle);
		}

		#endregion
	}
}
