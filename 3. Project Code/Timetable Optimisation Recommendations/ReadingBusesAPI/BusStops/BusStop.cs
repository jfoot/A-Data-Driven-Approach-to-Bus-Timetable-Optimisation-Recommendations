// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.JourneyDetails;
using ReadingBusesAPI.TimeTable;

namespace ReadingBusesAPI.BusStops
{
	/// <summary>
	///     Stores information about a single bus stop. Related to the "List Of Bus Stops" API.
	/// </summary>
	public sealed class BusStop
	{
		/// <summary>
		///     The default constructor used for parsing data automatically.
		/// </summary>
		internal BusStop()
		{
		}

		/// <summary>
		///     Used to create a snub/ fake object for passing to function calls, if all you need to pass is an acto-code to the
		///     function.
		/// </summary>
		/// <param name="atcoCode">ID of the bus stop.</param>
		/// <remarks>
		///     Unless you are doing something very strange, you probably should not need to use this, it is more for testing
		///     purposes.
		/// </remarks>
		public BusStop(string atcoCode)
		{
			AtcoCode = atcoCode;
		}

		/// <value>The unique identifier for a bus stop.</value>
		[JsonProperty("location_code")]
		public string AtcoCode { get; internal set; }

		/// <value>The public, easy to understand stop name.</value>
		[JsonProperty("description")]
		public string CommonName { get; internal set; }

		/// <value>The latitude of the bus stop</value>
		[JsonProperty("latitude")]
		public string Latitude { get; internal set; }

		/// <value>The longitude of the bus stop</value>
		[JsonProperty("longitude")]
		public string Longitude { get; internal set; }

		/// <value>The bearing of the bus stop</value>
		[JsonProperty("bearing")]
		public string Bearing { get; internal set; }

		/// <value>The services that travel to this stop, separated by '/'</value>
		/// See
		/// <see cref="BusStop.GetServices(Operators)" />
		/// to get a list of Service Objects.
		[JsonProperty("routes")]
		public string ServicesString { get; internal set; }

		/// <value>The Brand/Group of buses that most frequently visit this stop. Such as Purple, for the Purple 17s.</value>
		[JsonProperty("group_name")]
		public string GroupName { get; internal set; }

		/// <summary>
		///     Gets live data from a bus stop.
		/// </summary>
		/// <returns>Returns a list of Live Records, which are individual buses due to arrive at the bus stop.</returns>
		public async Task<LiveRecord[]> GetLiveData()
		{
			return await Task.Run(() => LiveRecord.GetLiveData(AtcoCode)).ConfigureAwait(false);
		}

		/// <summary>
		///     Finds the 'BusService' object for all of the bus services which visit this stop.
		/// </summary>
		/// <param name="busOperator"></param>
		/// <returns>A list of BusService Objects for services which visit this bus stop.</returns>
		public BusService[] GetServices(Company busOperator)
		{
			string[] services = ServicesString.Split('/');
			List<BusService> serviceObjects = new List<BusService>();

			foreach (var service in services)
			{
                if(ReadingBuses.GetInstance().IsService(service, busOperator))
				    serviceObjects.Add(ReadingBuses.GetInstance().GetService(service, busOperator));
			}

			return serviceObjects.ToArray();
		}

		/// <summary>
		///     Gets the geographical position of the bus stop.
		/// </summary>
		/// <returns>A Point Object for the position of the bus stop.</returns>
		public Point GetPoint() => new Point(double.Parse(Longitude), double.Parse(Latitude));


		/// <summary>
		///     Gets time table data at this specific bus stop.
		/// </summary>
		/// <param name="date">The date you want time table data for.</param>
		/// <returns>An array of time table records for a particular bus stop.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have not provided any date.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		public Task<BusTimeTable[]> GetTimeTable(DateTime date)
		{
			return BusTimeTable.GetTimeTable(null, date, this);
		}


		/// <summary>
		///     Gets time table data at this specific bus stop.
		/// </summary>
		/// <param name="date">The date you want time table data for.</param>
		/// <param name="service">
		///     (optional) the service you want time table data for specifically. If null, you get time table
		///     data for all services at this stop.
		/// </param>
		/// <returns>An array of time table records for a particular bus stop.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have not provided any date.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		public Task<BusTimeTable[]> GetTimeTable(DateTime date, BusService service)
		{
			return BusTimeTable.GetTimeTable(service, date, this);
		}


		/// <summary>
		///     Gets the archived real bus departure and arrival times along with their time table history at this specific bus
		///     stop.
		/// </summary>
		/// <param name="date">The date you want time table data for. This should be a date in the past.</param>
		/// <returns></returns>
		public Task<ArchivedBusTimeTable[]> GetArchivedTimeTable(DateTime date)
		{
			return ArchivedBusTimeTable.GetTimeTable(null, date, this, null);
		}


		/// <summary>
		///     Gets the archived real bus departure and arrival times along with their time table history at this specific bus
		///     stop.
		/// </summary>
		/// <param name="date">The date you want time table data for. This should be a date in the past.</param>
		/// <param name="service">
		///     (optional) the service you want time table data for specifically. If null, you get time table
		///     data for all services at this stop.
		/// </param>
		/// <returns></returns>
		public Task<ArchivedBusTimeTable[]> GetArchivedTimeTable(DateTime date, BusService service)
		{
			return ArchivedBusTimeTable.GetTimeTable(service, date, this, null);
		}
	}
}
