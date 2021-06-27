// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using ReadingBusesAPI.Common;

namespace ReadingBusesAPI.ErrorManagement
{
	/// <summary>
	///     Represents an error message object returned by all JSON feeds of the API.
	/// </summary>
	internal sealed class ErrorFormat
	{
		/// <value>The status of the request, always false for failed.</value>
		[JsonProperty("status")]
		public bool Status { get; set; }

		/// <value>The status code of the error</value>
		[JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(ParseStringConverter))]
		public long? Code { get; set; }

		/// <value>The error message/ reason.</value>
		[JsonProperty("message")]
		public string Message { get; set; }
	}
}
