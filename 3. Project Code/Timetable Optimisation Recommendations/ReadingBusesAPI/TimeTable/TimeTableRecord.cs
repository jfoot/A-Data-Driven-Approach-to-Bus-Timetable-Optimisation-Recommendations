// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;
using ReadingBusesAPI.Common;

namespace ReadingBusesAPI.TimeTable
{
	/// <summary>
	///     Represents the Raw timetable object data you get from the Timetabled Journeys and Tracking History APIs.
	/// </summary>
	public abstract class TimeTableRecord
	{
		/// <summary>
		///     Default constructor, to block creating objects directly.
		/// </summary>
		protected internal TimeTableRecord()
		{
		}

		/// <value>The service number of the bus.</value>
		[JsonProperty("LineRef")]
		internal string ServiceNumber { get; set; }


		/// <value>The operator of the bus services</value>
		[JsonProperty("Operator")]
		[JsonConverter(typeof(ParseOperatorConverter))]
		public Company Operator { get; internal set; }

		/// <value>The 'BusStop' object for the stop relating to the time table record..</value>
		[JsonProperty("LocationCode")]
		[JsonConverter(typeof(ParseBusStopConverter))]
		public BusStop Location { get; internal set; }

		/// <value>What number bus stop is this in the buses route, ie 1, is the first stop to visit.</value>
		[JsonProperty("Sequence")]
		[JsonConverter(typeof(ParseStringConverter))]
		public long Sequence { get; internal set; }

		/// <value>Is this bus heading inbound or outbound.</value>
		[JsonProperty("Direction")]
		[JsonConverter(typeof(ParseDirectionConverter))]
		public Direction Direction { get; internal set; }

		/// <value>
		///     A unique value that groups a selection of time table records across different bus stops to show one loop/ cycle
		///     of a bus services route.
		/// </value>
		[JsonProperty("JourneyCode")]
		public string JourneyCode { get; internal set; }


        /// <value>
        ///     A running board value, represents a group of journeys that one driver is expected to perform.
        ///     These are therefore sequential services, driven using the same vehicle.
        /// </value>
        [JsonProperty("RunningBoard")]
        public string RunningBoard { get; internal set; }


        /// <value>Is this bus stop a timing point or not.</value>
        /// <remarks>
        ///     A timing point is a major bus stop, where the buses is expected to wait if its early and should actually arrive on
        ///     the scheduled time.
        ///     All non-timing points times are only estimated scheduled times. A timing point is much more accurate and strict
        ///     timings.
        /// </remarks>
        [JsonProperty("TimingPoint")]
		[JsonConverter(typeof(ParseTimingPointConverter))]
		public bool IsTimingPoint { get; internal set; }

		/// <value>The scheduled arrival time for the bus. </value>
		[JsonProperty("ScheduledArrivalTime")]
		public DateTime SchArrivalTime { get; internal set; }

		/// <value>The scheduled departure time for the bus. </value>
		[JsonProperty("ScheduledDepartureTime")]
		public DateTime SchDepartureTime { get; internal set; }


		/// <summary>
		///     Gets the related 'BusService' object relating to the time table record.
		/// </summary>
		/// <returns>A 'BusService' object for this time table record.</returns>
        /// <remarks>
        ///     This will return back a stub object if the timetable record is historic and the service has been discontinued.
        ///     This is means that the service is no longer actively in the Services API. But an old reference has been found.
        /// </remarks>
		public BusService GetService()
		{
            if (ReadingBuses.GetInstance().IsService(ServiceNumber, Operator))
                return ReadingBuses.GetInstance().GetService(ServiceNumber, Operator);
            //Stub object for a discontinued service. 
            else
                return new BusService(ServiceNumber, Operator);
		}


		#region JSONConverters

		/// <summary>
		///     Converts a bus stop acto-code into a 'BusStop' Object and back again for the JSON converter.
		/// </summary>
		private class ParseBusStopConverter : JsonConverter
		{
			public static readonly ParseBusStopConverter Singleton = new ParseBusStopConverter();
			public override bool CanConvert(Type t) => t == typeof(BusStop);

			public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
				{
					return null;
				}

				var value = serializer.Deserialize<string>(reader);

				return ReadingBuses.GetInstance().GetLocation(value);
			}

			public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
			{
				if (untypedValue == null)
				{
					serializer.Serialize(writer, null);
					return;
				}

				var value = (BusStop)untypedValue;
				serializer.Serialize(writer, value.AtcoCode);
			}
		}


		/// <summary>
		///     Converts a string into a Direction Enum and back again for the JSON converter.
		/// </summary>
		private class ParseDirectionConverter : JsonConverter
		{
			public static readonly ParseDirectionConverter Singleton = new ParseDirectionConverter();
			public override bool CanConvert(Type t) => t == typeof(Direction) || t == typeof(Direction?);

			public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
				{
					return null;
				}

				var value = serializer.Deserialize<string>(reader);

				if (value.Equals("Inbound", StringComparison.OrdinalIgnoreCase))
				{
					return Direction.Inbound;
				}

				if (value.Equals("Outbound", StringComparison.OrdinalIgnoreCase))
				{
					return Direction.Outbound;
				}

				throw new JsonSerializationException("Cannot unmarshal type Direction");
			}

			public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
			{
				if (untypedValue == null)
				{
					serializer.Serialize(writer, null);
					return;
				}

				var value = (Direction)untypedValue;
				switch (value)
				{
					case Direction.Inbound:
						serializer.Serialize(writer, "Inbound");
						return;
					case Direction.Outbound:
						serializer.Serialize(writer, "Outbound");
						return;
				}

				throw new JsonSerializationException("Cannot marshal type Direction");
			}
		}

		/// <summary>
		///     Converts a string into a boolean and back again for the JSON converter.
		/// </summary>
		private class ParseTimingPointConverter : JsonConverter
		{
			public static readonly ParseTimingPointConverter Singleton = new ParseTimingPointConverter();
			public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

			public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
				{
					return null;
				}

				var value = serializer.Deserialize<string>(reader);

				if (value.Equals("NonTimingPoint", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				if (value.Equals("TimingPoint", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}

				throw new JsonSerializationException("Cannot unmarshal type TimingPoint");
			}

			public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
			{
				if (untypedValue == null)
				{
					serializer.Serialize(writer, null);
					return;
				}

				var value = (bool)untypedValue;
				switch (value)
				{
					case false:
						serializer.Serialize(writer, "NonTimingPoint");
						return;
					case true:
						serializer.Serialize(writer, "TimingPoint");
						return;
				}
			}
		}

		#endregion
	}
}
