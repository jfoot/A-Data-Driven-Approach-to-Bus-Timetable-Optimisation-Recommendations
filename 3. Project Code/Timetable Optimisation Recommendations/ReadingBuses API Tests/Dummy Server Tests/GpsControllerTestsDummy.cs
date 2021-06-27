// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ReadingBusesAPI;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.VehiclePositions;

namespace ReadingBuses_API_Tests.Dummy_Server_Tests
{
	/// <summary>
	///     Tests for the 'GPSController' class on a dummy server.
	/// </summary>
	[TestFixture]
	internal class GpsControllerTestsDummy
	{
		/// <summary>
		///     Directs traffic to live server.
		/// </summary>
		[OneTimeSetUp]
		public async void Setup()
		{
			//Do not use the dummy server connection. Actually connect to the real ones for these tests.
			await ReadingBuses.SetDebuggingAsync(true);
		}


		/// <summary>
		///     Checks is vehicle works correctly.
		/// </summary>
		[Test]
		public async Task CheckIsVehicle()
		{
			Assert.AreEqual(true, await ReadingBuses.GetInstance().GpsController.IsVehicle("1208"));
			Assert.AreEqual(false, await ReadingBuses.GetInstance().GpsController.IsVehicle("999"));
		}


		/// <summary>
		///     Checks we can get archived vehicle position data.
		/// </summary>
		[Test]
		public async Task GetArchivedVehiclePositionsAsync()
		{
			ArchivedPositions[] positions = await ReadingBuses.GetInstance().GpsController
				.GetArchivedVehiclePositions(DateTime.Now.AddDays(-1), new TimeSpan(3, 0, 0));
			if (positions.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail(
					"No archived vehicles found. But this could be because there isn't any, please run this test at a reasonable British time.");
			}
		}

		/// <summary>
		///     Throws an error is a null time span or date in the future is passed.
		/// </summary>
		[Test]
		public void GetArchivedVehiclePositionsErrorAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionMalformedQuery>(async () =>
				await ReadingBuses.GetInstance().GpsController
					.GetArchivedVehiclePositions(DateTime.Now.AddDays(-1), null));

			Assert.ThrowsAsync<ReadingBusesApiExceptionMalformedQuery>(async () =>
				await ReadingBuses.GetInstance().GpsController
					.GetArchivedVehiclePositions(DateTime.Now.AddDays(10), new TimeSpan(5, 0, 0)));
		}


		/// <summary>
		///     Checks we can get live vehicle data.
		/// </summary>
		[Test]
		public async Task GetLiveVehiclePositionsAsync()
		{
			LivePosition[] positions = await ReadingBuses.GetInstance().GpsController.GetLiveVehiclePositions();
			if (positions.Length == 0)
			{
				Assert.Fail("No live vehicles found.");
			}


			//If this doesn't throw an error it has worked.
			_ =
				await ReadingBuses.GetInstance().GpsController.GetLiveVehiclePosition("1208");

			Assert.Pass();
		}

		/// <summary>
		///     Checks null is returned when the data doesn't exist.
		/// </summary>
		[Test]
		public void GetVehiclePositionsInvalidIdAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionBadQuery>(async () =>
				await ReadingBuses.GetInstance().GpsController.GetLiveVehiclePosition("999"));
		}
	}
}
