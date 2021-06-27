// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;

namespace ReadingBusesAPI.ErrorManagement
{
	/// <summary>
	///     An exception type which is used when the user asks to make a invalid API call
	///     This is would be thrown during checks done before even directly calling upon the web API.
	///     For example if you have not filtered by at least one property when required too.
	/// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
	public class ReadingBusesApiExceptionMalformedQuery : ReadingBusesApiException
#pragma warning restore CA1032 // Implement standard exception constructors
	{
		internal ReadingBusesApiExceptionMalformedQuery(string content) : base(content)
		{
		}

		internal ReadingBusesApiExceptionMalformedQuery(string message, Exception innerException) : base(message,
			innerException)
		{
		}
	}
}
