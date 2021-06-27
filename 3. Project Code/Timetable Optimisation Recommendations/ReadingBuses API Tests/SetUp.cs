// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ReadingBusesAPI;

namespace ReadingBuses_API_Tests
{
	/// <summary>
	///     Sets up the testing framework and initialise the library.
	/// </summary>
	[SetUpFixture]
	internal class SetUp
	{
		[OneTimeSetUp]
		public async Task SetupAsync()
		{
			//Do not use any cached data always get fresh data.
			ReadingBuses.SetCache(false);
			//Output any errors to logs for debugging them.
			ReadingBuses.SetFullError(true);
			//Output any warnings to logs for debugging them if needed.
			ReadingBuses.SetWarning(true);
            //By Default use the dummy server first. We can switch to live server in later tests.
            await ReadingBuses.SetDebuggingAsync(true);

            //Gets the API Key from Git Hub Secrets. 
            _ = await ReadingBuses.Initialise(Environment.GetEnvironmentVariable("API_KEY"));
		}
	}
}
