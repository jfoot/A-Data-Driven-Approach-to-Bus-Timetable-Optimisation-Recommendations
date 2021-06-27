// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;
using ReadingBusesAPI.Common;

namespace ReadingBusesAPI.VehiclePositions
{
	/// <summary>
	///     Stores information about previous/ archived GPS data on vehicles.
	/// </summary>
	public class ArchivedPositions
	{
		/// <summary>
		///     The default constructor, which sets the 'LastRetrieval' to current time.
		/// </summary>
		internal ArchivedPositions()
		{
		}


		/// <value>Holds the operators enum value.</value>
		[JsonProperty("operator")]
		[JsonConverter(typeof(ParseOperatorConverter))]
		public Company OperatorCode { get; internal set; }

		/// <value>Holds the reference/identifier for the vehicle</value>
		[JsonProperty("vehicle")]
		public string Vehicle { get; internal set; }

		/// <value>Holds the time it was last seen/ new data was retrieved.</value>
		[JsonProperty("observed")]
		public DateTimeOffset Observed { get; internal set; }

		/// <value>Latitude position of the bus</value>
		[JsonProperty("latitude")]
		public string Latitude { get; internal set; }

		/// <value>longitude position of the bus</value>
		[JsonProperty("longitude")]
		public string Longitude { get; internal set; }


		/// <summary>
		///     Gets the geographical position of the bus.
		/// </summary>
		/// <returns>A Point Object for the position of the bus.</returns>
		public Point GetPoint() => new Point(double.Parse(Longitude), double.Parse(Latitude));
	}
}
