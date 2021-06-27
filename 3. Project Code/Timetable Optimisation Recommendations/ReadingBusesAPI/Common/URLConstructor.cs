using System;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;

namespace ReadingBusesAPI.Common
{
	/// <summary>
	///     Returns back the URL needed to make a get command to the Reading Buses Open Data API.
	///     You can use this for testing purposes to check the API is returning what you were expecting.
	/// </summary>
	public static class UrlConstructor
	{
		/// <value>URL for the Reading buses Open Data API server.</value>
		private const string ReadingBusesApi = "https://rtl2.ods-live.co.uk/api/";

		/// <value>URL for the Reading buses Open Data API server.</value>
		private const string DummyApi = "https://jonathanfoot.com/Projects/RBAPI/test-data/";


		/// <summary>
		///     Returns back the URL needed for a get request to the 'List of Bus Stops' API.
		/// </summary>
		/// <returns> Returns back the URL needed for a get request to the 'List of Bus Stops' API.</returns>
		public static string ListOfBusStops()
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "busstops.html";
			}

			return ReadingBusesApi + "busstops?key=" + ReadingBuses.ApiKey;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Live Vehicle Positions' API.
		/// </summary>
		/// <returns> Returns back the URL needed for a get request to the 'Live Vehicle Positions' API.</returns>
		public static string LiveVehiclePositions()
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "vehiclePositions.html";
			}

			return ReadingBusesApi + "vehiclePositions?key=" + ReadingBuses.ApiKey;
		}


		/// <summary>
		///     Returns back the URL needed for a get request to the 'Live Journey Details' API.
		/// </summary>
		/// <returns>Returns back the URL needed for a get request to the 'Live Journey Details' API.</returns>
		public static string LiveJourneyDetails() =>
			throw new NotImplementedException("This API feed is not yet implemented");

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Stop Predictions' API.
		/// </summary>
		/// <param name="actoCode">The bus stop ID code.</param>
		/// <returns>Returns back the URL needed for a get request to the 'Stop Predictions' API.</returns>
		public static string StopPredictions(string actoCode)
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "stopPredictions.xml";
			}

			return ReadingBusesApi + "siri/sm?key=" + ReadingBuses.ApiKey +
			       "&location=" + actoCode;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'List of Services' API.
		/// </summary>
		/// <returns>Returns back the URL needed for a get request to the 'List of Services' API.</returns>
		public static string ListOfServices()
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "services.html";
			}

			return ReadingBusesApi + "services?key=" + ReadingBuses.ApiKey;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Line Patterns' API.
		/// </summary>
		/// <param name="service">The bus service to query for.</param>
		/// <returns>Returns back the URL needed for a get request to the 'Line Patterns' API.</returns>
		public static string LinePatterns(BusService service)
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "linePatterns.html";
			}

			return ReadingBusesApi + "linePatterns?key=" + ReadingBuses.ApiKey + "&service=" + service.ServiceId;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Time Tabled Journeys' API.
		/// </summary>
		/// <param name="service">The bus service to query for.</param>
		/// <param name="location">The bus stop to query for.</param>
		/// <param name="date">The date you want to query at.</param>
		/// <returns> Returns back the URL needed for a get request to the 'Time Tabled Journeys' API.</returns>
		public static string TimetabledJourneys(BusService service, BusStop location, DateTime date)
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "scheduledJourneys.html";
			}

			return ReadingBusesApi + "scheduledJourneys?key=" + ReadingBuses.ApiKey +
			       "&service=" + (service ?? new BusService("")).ServiceId +
			       "&date=" +
			       date.ToString("yyyy-MM-dd") + "&location=" +
			       (location ?? new BusStop("")).AtcoCode;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Tracking History' API.
		/// </summary>
		/// <param name="service">The bus service to query for.</param>
		/// <param name="location">The bus stop to query for.</param>
		/// <param name="date">The date you want to query at.</param>
		/// <param name="vehicle">A vehicle ID to query for.</param>
		/// <returns> Returns back the URL needed for a get request to the 'Tracking History' API.</returns>
		public static string TrackingHistory(BusService service, BusStop location, DateTime date, string vehicle)
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "trackingHistory.html";
			}

			return ReadingBusesApi + "trackingHistory?key=" +
			       ReadingBuses.ApiKey +
			       "&service=" + (service ?? new BusService("")).ServiceId +
			       "&date=" +
			       date.ToString("yyyy-MM-dd") + "&vehicle=" + vehicle +
			       "&location=" +
			       (location ?? new BusStop("")).AtcoCode;
		}

		/// <summary>
		///     Returns back the URL needed for a get request to the 'Vehicle Position History' API.
		/// </summary>
		/// <param name="dateStartTime">The date and time you want to query for.</param>
		/// <param name="timeSpan">The length of the time period to query for.</param>
		/// <param name="vehicle">A vehicle ID to query for.</param>
		/// <returns>Returns back the URL needed for a get request to the 'Vehicle Position History' API.</returns>
		public static string VehiclePositionHistory(DateTime dateStartTime, TimeSpan? timeSpan,
			string vehicle)
		{
			if (ReadingBuses.Debugging)
			{
				return DummyApi + "vehiclePositionHistory.html";
			}

			return ReadingBusesApi + "vehiclePositionHistory?key=" + ReadingBuses.ApiKey +
			       "&date=" + dateStartTime.ToString("yyyy-MM-dd") + "&vehicle=" + vehicle + "&from=" +
			       dateStartTime.TimeOfDay +
			       "&to=" + AddTimeSpan(dateStartTime, timeSpan).TimeOfDay;
		}


		/// <summary>
		///     Adds the time span onto the start date time. If the time span expands into the next day stop it and limit it to
		///     today only. If no time span was given then assume they want a full day of data.
		/// </summary>
		/// <param name="start">The start date time, for what day and what time they want to get data from.</param>
		/// <param name="timeSpan">
		///     The length of time you want data for. Must be a positive value. When added to 'start' it should
		///     not take you into the next day.
		/// </param>
		/// <returns>The new DateTime object, which has the start date time object incremented by the time span safely.</returns>
		private static DateTime AddTimeSpan(DateTime start, TimeSpan? timeSpan)
		{
			if (timeSpan == null)
			{
				return start.Date + new TimeSpan(23, 59, 59);
			}

			//Since we have already checked it is not null we can cast it.
			TimeSpan safe = (TimeSpan)timeSpan;

			DateTime newDateTime = start + safe.Duration();
			if (newDateTime.Date.Equals(start.Date))
			{
				return newDateTime;
			}

			return start.Date + new TimeSpan(23, 59, 59);
		}
	}
}
