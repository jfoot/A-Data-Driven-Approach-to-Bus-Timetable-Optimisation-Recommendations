// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;

namespace ReadingBusesAPI.VehiclePositions
{
	/// <summary>
	///     Helps get live and historical GPS data on vehicles by accessing the "Live Vehicle Positions" API.
	/// </summary>
	public sealed class GpsController
	{
		/// <value>The last time a GPS request was made. This is used to prevent unnecessary API calls.</value>
		private static DateTime _lastRetrieval;

		/// <value>Holds the cache data for live GPS of vehicles.</value>
		private LivePosition[] _livePositionCache;

		/// <summary>
		///     Creates a GPS Controller, you should not need to make your own GPS controller, you can get an instance of one via
		///     the main 'ReadingBuses' object.
		/// </summary>
		internal GpsController()
		{
			_lastRetrieval = DateTime.Now.AddHours(-1);
		}

		/// <summary>
		///     GPS data only updates every 30 seconds, so on average you will need to wait 15s for new data.
		///     This is used to check how long it was since last requesting GPS data. If it was recently
		///     there  is no point making another request to the API as you will get the same data and take longer.
		/// </summary>
		/// <returns>Returns if it has been less than 15 seconds from last asking for GPS data.</returns>
		private static bool IsCacheValid() => (DateTime.Now - _lastRetrieval).TotalSeconds > 15;


		/// <summary>
		///     Gets historic/archived GPS data for buses on a specific date, filtered either by vehicle ID, or all buses without a
		///     time period or both.
		///     GPS data is not stored for as long as other forms of data you may fail to get data older than a few months.
		/// </summary>
		/// <param name="dateStartTime">Vehicle ID Number eg 414</param>
		/// <param name="timeSpan">
		///     (optional) How long a period do you want data for, you can not get multiple days worth of data.
		///     If you ask this your result will be automatically truncated to only the start date to midnight.
		/// </param>
		/// <returns>An array of GPS locations at a previous date.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if, you have not choose a date in the past, or the date is too far in the past and so no data exists.
		///     Thrown if you have not filtered by either 'timeSpan' or 'vehicle' ID or both.
		///     Thrown if the API key is invalid or expired.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		/// See
		/// <see cref="GpsController.GetLiveVehiclePositions()" />
		/// to get live data instead.
#pragma warning disable CA1822 // Mark members as static
		public async Task<ArchivedPositions[]> GetArchivedVehiclePositions(DateTime dateStartTime, TimeSpan? timeSpan)
		{
#pragma warning restore CA1822 // Mark members as static
			return await GetArchivedVehiclePositions(dateStartTime, timeSpan, null).ConfigureAwait(false);
		}


		/// <summary>
		///     Gets historic/archived GPS data for buses on a specific date, filtered either by vehicle ID, or all buses without a
		///     time period or both.
		///     GPS data is not stored for as long as other forms of data you may fail to get data older than a few months.
		/// </summary>
		/// <param name="dateStartTime">Vehicle ID Number eg 414</param>
		/// <param name="timeSpan">
		///     (optional) How long a period do you want data for, you can not get multiple days worth of data.
		///     If you ask this your result will be automatically truncated to only the start date to midnight.
		/// </param>
		/// <param name="vehicle">(optional) Vehicle ID Number eg 414</param>
		/// <returns>An array of GPS locations at a previous date.</returns>
		/// <exception cref="ReadingBusesApiExceptionMalformedQuery">
		///     Thrown if, you have not choose a date in the past, or the date is too far in the past and so no data exists.
		///     Thrown if you have not filtered by either 'timeSpan' or 'vehicle' ID or both.
		///     Thrown if the API key is invalid or expired.
		/// </exception>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API responds with an error message.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		/// See
		/// <see cref="GpsController.GetLiveVehiclePositions()" />
		/// to get live data instead.
#pragma warning disable CA1822 // Mark members as static
		public async Task<ArchivedPositions[]> GetArchivedVehiclePositions(DateTime dateStartTime, TimeSpan? timeSpan,
#pragma warning restore CA1822 // Mark members as static
			string vehicle)
		{
			if (dateStartTime == null || dateStartTime > DateTime.Now)
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"You can not get past data for a date in the future, if you want real time GPS data please us the 'GetLiveVehiclePositions' Function instead.");
			}

			if (timeSpan == null && string.IsNullOrEmpty(vehicle))
			{
				throw new ReadingBusesApiExceptionMalformedQuery(
					"You must filter by either timeSpan and/or vehicle ID. Both can not be left blank.");
			}


			var json =
				await new WebClient().DownloadStringTaskAsync(
						new Uri(UrlConstructor.VehiclePositionHistory(dateStartTime, timeSpan, vehicle)))
					.ConfigureAwait(false);

			try
			{
				ArchivedPositions[] data = JsonConvert.DeserializeObject<ArchivedPositions[]>(json).ToArray();
				return data;
			}
			catch (JsonSerializationException)
			{
				ErrorManager.TryErrorMessageRetrieval(json);
			}

			//Should never reach this stage.
			throw new ReadingBusesApiExceptionCritical();
		}


		/// <summary>
		///     Gets live GPS data for all buses currently operating.
		/// </summary>
		/// <returns>An array of GPS locations for all buses operating by Reading Buses currently</returns>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">Thrown if the API key is invalid or expired.</exception>
		/// <exception cref="ReadingBusesApiExceptionCritical">Thrown if the API fails, but provides no reason.</exception>
		public async Task<LivePosition[]> GetLiveVehiclePositions()
		{
			if (IsCacheValid() || _livePositionCache == null)
			{
				var json =
					await new WebClient().DownloadStringTaskAsync(
						new Uri(UrlConstructor.LiveVehiclePositions())).ConfigureAwait(false);

				try
				{
					_livePositionCache = JsonConvert.DeserializeObject<LivePosition[]>(json).ToArray();
					_lastRetrieval = DateTime.Now;
					return _livePositionCache;
				}
				catch (JsonSerializationException)
				{
					ErrorManager.TryErrorMessageRetrieval(json);
				}

				//Should never get here.
				throw new ReadingBusesApiExceptionCritical();
			}

			return _livePositionCache;
		}

		/// <summary>
		///     Gets live GPS data for a single buses matching Vehicle ID number.
		/// </summary>
		/// <param name="vehicle">Vehicle ID Number eg 414</param>
		/// <returns>The GPS point of Vehicle matching your ID provided.</returns>
		/// <exception cref="ReadingBusesApiExceptionBadQuery">
		///     Thrown if a vehicle of the ID does not exist or is not currently active.
		///     You can check by using the 'IsVehicle' function.
		/// </exception>
		public async Task<LivePosition> GetLiveVehiclePosition(string vehicle)
		{
			if (await IsVehicle(vehicle).ConfigureAwait(false))
			{
				return (await GetLiveVehiclePositions().ConfigureAwait(false)).Single(o =>
					string.Equals(o.Vehicle, vehicle, StringComparison.CurrentCultureIgnoreCase));
			}

			throw new ReadingBusesApiExceptionBadQuery(
				"A Vehicle of that ID can not be found currently operating. You can first check with the 'IsVehicle' function.");
		}

		/// <summary>
		///     Checks if the Vehicle ID Number is currently in service right now.
		/// </summary>
		/// <param name="vehicle">Vehicle ID Number eg 414</param>
		/// <returns>True or False for if the buses GPS can be found or not currently.</returns>
		public async Task<bool> IsVehicle(string vehicle) =>
			(await GetLiveVehiclePositions().ConfigureAwait(false)).Any(o =>
				string.Equals(o.Vehicle, vehicle, StringComparison.CurrentCultureIgnoreCase));
	}
}
