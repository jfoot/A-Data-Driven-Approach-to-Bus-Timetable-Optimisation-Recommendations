// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;

namespace ReadingBusesAPI.Common
{
	/// <summary>
	///     Converts a string short code for an Operator into an Operator Enum and back again for the JSON converter.
	/// </summary>
	internal class ParseOperatorConverter : JsonConverter
	{
		public static readonly ParseOperatorConverter Singleton = new ParseOperatorConverter();
		public override bool CanConvert(Type t) => t == typeof(Company) || t == typeof(Company?);

		public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			var value = serializer.Deserialize<string>(reader);

			return ReadingBuses.GetOperatorE(value);
		}

		public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
		{
			if (untypedValue == null)
			{
				serializer.Serialize(writer, null);
				return;
			}

			var value = (Company)untypedValue;

			switch (value)
			{
				case Company.ReadingBuses:
					serializer.Serialize(writer, "RGB");
					return;
				case Company.Kennections:
					serializer.Serialize(writer, "KC");
					return;
				case Company.NewburyAndDistrict:
					serializer.Serialize(writer, "N&D");
					return;
				default:
					serializer.Serialize(writer, "OTH");
					return;
			}

			throw new JsonSerializationException("Cannot marshal type Operators");
		}
	}


	/// <summary>
	///     Converts a string into a long and back again for the JSON converter.
	/// </summary>
	internal class ParseStringConverter : JsonConverter
	{
		public static readonly ParseStringConverter Singleton = new ParseStringConverter();
		public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

		public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			var value = serializer.Deserialize<string>(reader);
			if (long.TryParse(value, out var l))
			{
				return l;
			}

			throw new JsonSerializationException("Cannot unmarshal type long");
		}

		public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
		{
			if (untypedValue == null)
			{
				serializer.Serialize(writer, null);
				return;
			}

			var value = (long)untypedValue;
			serializer.Serialize(writer, value.ToString());
		}
	}
}
