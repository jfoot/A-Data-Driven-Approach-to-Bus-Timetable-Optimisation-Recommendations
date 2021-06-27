// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using ReadingBusesAPI.BusServices;

namespace ReadingBusesAPI.VehiclePositions
{
	/// <summary>
	///     Used to store live information about a buses GPS position. Related to the "Live Vehicle Positions" API.
	/// </summary>
	public sealed class LivePosition : ArchivedPositions
	{
		/// <summary>
		///     The default constructor, which sets the 'LastRetrieval' to current time.
		/// </summary>
		internal LivePosition()
		{
		}

		/// <value>Holds the Service Number for the bus route.</value>
		[JsonProperty("service")]
		public string ServiceId { get; internal set; }

		/// <value>bearing direction of the bus</value>
		[JsonProperty("bearing")]
		public string Bearing { get; internal set; }


		/// <summary>
		///     Finds the 'BusService' object related to the record.
		/// </summary>
		/// <returns>The related 'BusService' object.</returns>
		public BusService GetService() => ReadingBuses.GetInstance().GetService(ServiceId, OperatorCode);
	}
}
