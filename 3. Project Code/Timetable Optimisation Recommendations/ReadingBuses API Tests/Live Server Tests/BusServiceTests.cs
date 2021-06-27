// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ReadingBusesAPI;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.TimeTable;
using ReadingBusesAPI.VehiclePositions;

namespace ReadingBuses_API_Tests.Live_Server_Tests
{
	/// <summary>
	///     Tests for the 'BusService' class on the live server.
	/// </summary>
	[TestFixture]
	internal class BusServiceTests
	{
		private BusService _testService;

		/// <summary>
		///     Directs traffic to dummy server.
		/// </summary>
		[OneTimeSetUp]
		public async void Setup()
		{
			//Use the Live server connection. 
			await ReadingBuses.SetDebuggingAsync(false);
			_testService = ReadingBuses.GetInstance().GetService("17", Company.ReadingBuses);
		}


		/// <summary>
		///     Check the default constructor
		/// </summary>
		[Test]
		public void CheckDefaultConstructor()
		{
			BusService service = new BusService("22");

			Assert.AreEqual("22", service.ServiceId);
			Assert.AreEqual(Company.Other, service.OperatorCode);
		}


		/// <summary>
		///     Check that an array of archived time table records is returned.
		/// </summary>
		[Test]
		public async Task CheckGetArchivedTimeTableAsync()
		{
			ArchivedBusTimeTable[] timeTable = await _testService.GetArchivedTimeTable(DateTime.Now.AddDays(-1));


			if (timeTable.Length == 0)
			{
				Assert.Fail("No time table records were returned.");
			}


			Assert.Pass();
		}


		/// <summary>
		///     Check that an error is thrown when trying to get future data.
		/// </summary>
		[Test]
		public void CheckGetArchivedTimeTableErrorAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionMalformedQuery>(async () =>
				await _testService.GetArchivedTimeTable(DateTime.Now.AddDays(10)));
		}


		/// <summary>
		///     Check that an array of archived time table records is returned and grouped correctly.
		/// </summary>
		[Test]
		public async Task CheckGetArchivedTimeTableGroupedAsync()
		{
			IGrouping<string, ArchivedBusTimeTable>[] timeTableGroup =
				await _testService.GetGroupedArchivedTimeTable(DateTime.Now.AddDays(-1));

			if (timeTableGroup.Length == 0)
			{
				Assert.Fail("No time table records were returned.");
			}

			foreach (var group in timeTableGroup)
			{
				if (@group.Any(x => x.JourneyCode != @group.First().JourneyCode))
				{
					Assert.Fail("Not all elements in group have same journey code.");
				}
			}

			Assert.Pass();
		}


		/// <summary>
		///     Check that an error is thrown when trying to get future data.
		/// </summary>
		[Test]
		public void CheckGetArchivedTimeTableGroupedErrorAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionMalformedQuery>(async () =>
				await _testService.GetGroupedArchivedTimeTable(DateTime.Now.AddDays(10)));
		}

		/// <summary>
		///     Check that an error is thrown when trying to get data from a non-existant locaiton.
		/// </summary>
		[Test]
		public void CheckGetArchivedTimeTableGroupedErrorLocationAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionBadQuery>(async () =>
				await _testService.GetGroupedArchivedTimeTable(DateTime.Now.AddDays(-1), new BusStop("999")));
		}

		/// <summary>
		///     Check that an error is thrown when trying to get data from a non-existant locaiton.
		/// </summary>
		[Test]
		public void CheckGetArchivedTimeTableLocationErrorAsync()
		{
			Assert.ThrowsAsync<ReadingBusesApiExceptionBadQuery>(async () =>
				await _testService.GetArchivedTimeTable(DateTime.Now.AddDays(-1), new BusStop("999")));
		}


		/// <summary>
		///     Check that an array of Live GPS positions is returned is returned.
		/// </summary>
		[Test]
		public async Task CheckGetLivePositionsAsync()
		{
			LivePosition[] livePositions = await _testService.GetLivePositions();


			if (livePositions.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No live positions were returned.");
			}
		}


		/// <summary>
		///     Check that an array of string of acto codes is returned.
		/// </summary>
		[Test]
		public async Task CheckGetLocationsActoCodesAsync()
		{
			string[] actoCodes = await _testService.GetLocationsActo();

			foreach (var actoCode in actoCodes)
			{
				if (!ReadingBuses.GetInstance().IsLocation(actoCode))
				{
					Assert.Fail("Not a real location.");
				}
			}


			if (actoCodes.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No acto-codes were returned.");
			}
		}


		/// <summary>
		///     Check that an array of locations is returned.
		/// </summary>
		[Test]
		public async Task CheckGetLocationsAsync()
		{
			BusStop[] locations = await _testService.GetLocations();


			if (locations.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No locations were returned.");
			}
		}


		/// <summary>
		///     Check that an array of time table records is returned is returned.
		/// </summary>
		[Test]
		public async Task CheckGetTimeTableAsync()
		{
			const string actoCode = "039027540001";
			BusTimeTable[] timeTable = await _testService.GetTimeTable(DateTime.Now.AddDays(-1));


			if (timeTable.Length == 0)
			{
				Assert.Fail("No time table records were returned.");
			}

			BusTimeTable[] timeTableAtLocation =
				await _testService.GetTimeTable(DateTime.Now.AddDays(-1), new BusStop(actoCode));

			foreach (var record in timeTableAtLocation)
			{
				if (!record.Location.AtcoCode.Equals(actoCode))
				{
					Assert.Fail("The time table record was not for the stop asked for.");
				}
			}

			Assert.Pass();
		}


		/// <summary>
		///     Check that an array of time table records is returned and grouped correctly.
		/// </summary>
		[Test]
		public async Task CheckGetTimeTableGroupedAsync()
		{
			IGrouping<string, BusTimeTable>[] timeTableGroup =
				await _testService.GetGroupedTimeTable(DateTime.Now.AddDays(-1));

			if (timeTableGroup.Length == 0)
			{
				Assert.Fail("No time table records were returned.");
			}

			foreach (var group in timeTableGroup)
			{
				if (@group.Any(x => x.JourneyCode != @group.First().JourneyCode))
				{
					Assert.Fail("Not all elements in group have same journey code.");
				}
			}

			Assert.Pass();
		}

		/// <summary>
		///     Check the second constructor
		/// </summary>
		[Test]
		public void CheckSecondConstructor()
		{
			BusService service = new BusService("22", Company.ReadingBuses);

			Assert.AreEqual("22", service.ServiceId);
			Assert.AreEqual(Company.ReadingBuses, service.OperatorCode);
		}
	}
}
