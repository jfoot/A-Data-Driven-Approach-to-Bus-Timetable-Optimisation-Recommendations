// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;

namespace ReadingBusesAPI.TimeTable
{
	/// <summary>
	///     Represents and retrieves information  about a actual single time table record, which means information on one bus
	///     at one location. Related to the "Tracking History" API.
	/// </summary>
	public class ArchivedBusTimeTable : TimeTableRecord
	{
		/// <summary>
		///     Default constructor to prevent creating an object directly outside the API.
		/// </summary>
		internal ArchivedBusTimeTable()
		{
		}

		/// <value>The actual arrival time for the bus. </value>
		[JsonProperty("ArrivalTime")]
		public DateTime? ActArrivalTime { get; set; }

		/// <value>The actual departure time for the bus. </value>
		[JsonProperty("DepartureTime")]
		public DateTime? ActDepartureTime { get; set; }

		/// <summary>
		///     How late the bus was to arrive at a bus stop.
		/// </summary>
		/// <returns>
		///     The number of seconds the bus was late to arrive by.
		///     If no arrival time can be found, 0 is returned.
		/// </returns>
		public double ArrivalLateness()
		{
			if (ActArrivalTime != null)
			{
				return ((DateTime)ActArrivalTime - SchArrivalTime).TotalSeconds;
			}

			return 0;
		}

		/// <summary>
		///     How late the bus was to departure at a bus stop.
		/// </summary>
		/// <returns>
		///     The number of seconds the bus was late to departure by.
		///     If no departure time can be found, 0 is returned.
		/// </returns>
		public double DepartureLateness()
		{
			if (ActDepartureTime != null)
			{
				return ((DateTime)ActDepartureTime - SchDepartureTime).TotalSeconds;
			}

			return 0;
		}


		/// <summary>
		///     Gets the actual arrival and departure times of a bus, by service, date, location and/or vehicle ID.
		/// </summary>
		/// <param name="service">The bus services you wish to view.</param>
		/// <param name="date">The date of the time table.</param>
		/// <param name="location">The location to get timetable data from.</param>
		/// <param name="vehicle">A bus/Vehicle ID number.</param>
		/// <returns>An array of time table records for the service or location or both</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have tried to get data for a date in the future. Or if you have not provided any date, and/or you have not
		///     provided at least either the service or location or vehicle.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		/// See also
		/// <see cref="BusTimeTable.GetTimeTable(BusService , DateTime ,BusStop)" />
		/// to get future time table data instead.
		internal static async Task<ArchivedBusTimeTable[]> GetTimeTable(BusService service, DateTime date,
			BusStop location, string vehicle)
		{
			if (date == null || date > DateTime.Now)
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"You can not get past data for a date in the future, if you want time table data use the 'BusTimeTable' objects and functions instead.");
			}

			if (service == null && location == null && string.IsNullOrEmpty(vehicle))
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"You must provide a date and a service and/or location for a valid query.");
			}

			string json = await new WebClient().DownloadStringTaskAsync(
				UrlConstructor.TrackingHistory(service, location, date, vehicle)).ConfigureAwait(false);

			try
			{
				var timeTable = JsonConvert.DeserializeObject<List<ArchivedBusTimeTable>>(json);
				return timeTable.ToArray();
			}
			catch (JsonSerializationException)
			{
				ErrorManager.TryErrorMessageRetrieval(json);
			}

			//Should never reach this stage.
			throw new ReadingBusesApiExceptionCritical();
		}


		/// <summary>
		///     Gets the time table for a service and groups it by a journey code instead of one continuous array of time table
		///     entries.
		/// </summary>
		/// <param name="service">The bus services you wish to view.</param>
		/// <param name="date">The date of the time table.</param>
		/// <param name="location">The location to get timetable data from.</param>
		/// <param name="vehicle">A bus/Vehicle ID number.</param>
		/// <returns>Returns an IGroupings of Arrays of 'BusTimeTable' records grouped by journey codes.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have tried to get data for a date in the future. Or if you have not provided any date, and/or you have not
		///     provided at least either the service or location or vehicle.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		internal static async Task<IGrouping<string, ArchivedBusTimeTable>[]> GetGroupedTimeTable(BusService service,
			DateTime date,
			BusStop location, string vehicle)
		{
			return (await GetTimeTable(service, date, location, vehicle).ConfigureAwait(false))
				.GroupBy(x => x.JourneyCode).ToArray();
		}
	}
}
