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
	///     Represents and retrieves information  about a scheduled/predicted single time table record, which means information
	///     on one bus at one location. Related
	///     to the "Timetabled Journeys" API.
	/// </summary>
	public class BusTimeTable : TimeTableRecord
	{
		/// <summary>
		///     Default constructor to prevent creating an object directly outside the API.
		/// </summary>
		internal BusTimeTable()
		{
		}


		/// <summary>
		///     Gets the time table of a service or a location as one array of 'BusTimeTable' objects.
		/// </summary>
		/// <param name="service">The bus services you wish to view.</param>
		/// <param name="date">The date of the time table.</param>
		/// <param name="location">The location to get timetable data from.</param>
		/// <returns>An array of time table records for the service or location or both</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have not provided any date, and/or you have not provided at least
		///     either the service or location.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		internal static async Task<BusTimeTable[]> GetTimeTable(BusService service, DateTime date,
			BusStop location)
		{

			if (date == null || (service == null && location == null))
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"You must provide a date and a service and/or location for a valid query.");
			}

         
            string json = await new WebClient().DownloadStringTaskAsync(
				UrlConstructor.TimetabledJourneys(service, location, date)).ConfigureAwait(false);

			try
			{
				var timeTable = JsonConvert.DeserializeObject<List<BusTimeTable>>(json);
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
		/// <returns>Returns an IGroupings of Arrays of 'BusTimeTable' records grouped by journey codes.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     If you have not provided any date, and/or you have not provided at least
		///     either the service or location.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		internal static async Task<IGrouping<string, BusTimeTable>[]> GetGroupedTimeTable(BusService service,
			DateTime date,
			BusStop location)
		{
			return (await GetTimeTable(service, date, location).ConfigureAwait(false))
				.GroupBy(x => x.JourneyCode).ToArray();
		}
	}
}
