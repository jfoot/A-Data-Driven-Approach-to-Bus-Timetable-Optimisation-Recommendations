// Copyright(c) Jonathan Foot.All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace ReadingBusesAPI.ErrorManagement
{
	/// <summary>
	///     Responsible for extracting and producing an error message from an API result. To the end user.
	/// </summary>
	internal static class ErrorManager
	{
		/// <summary>
		///     Attempts to extract the error message directly sent from the API.
		///     If it can get the error message given send to the user. If the error message
		///     can not be extracted then the API has proved no explanation and so throw a generic error.
		/// </summary>
		/// <param name="json">Data returned from the API.</param>
		internal static void TryErrorMessageRetrieval(string json)
		{
			ErrorFormat error;
			try
			{
				//If it can deserializeObject the object then the API has proved an error message.
				error = JsonConvert.DeserializeObject<ErrorFormat>(json);
			}
			catch (JsonSerializationException ex)
			{
				//Else it failed to extract an error message and so throw a generic critical error.
				ReadingBuses.PrintFullErrorLogs(ex.Message);
				throw new ReadingBusesApiExceptionCritical();
			}

			throw new ReadingBusesApiExceptionBadQuery(error);
		}
	}
}
