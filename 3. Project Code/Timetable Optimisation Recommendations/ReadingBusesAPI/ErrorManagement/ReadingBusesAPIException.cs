// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;

namespace ReadingBusesAPI.ErrorManagement
{
	/// <summary>
	///     Stores the basic/base type of Exception which can be thrown by the API.
	/// </summary>
	public class ReadingBusesApiException : Exception
	{
		internal ReadingBusesApiException()
		{
		}

		internal ReadingBusesApiException(string content) : base(content)
		{
		}

		internal ReadingBusesApiException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
