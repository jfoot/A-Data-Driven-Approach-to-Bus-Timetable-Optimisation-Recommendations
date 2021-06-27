// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ReadingBusesAPI;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.BusStops;
using ReadingBusesAPI.Common;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.TimeTable;

namespace ReadingBuses_API_Tests.Dummy_Server_Tests
{
	/// <summary>
	///     Tests for the 'BusStop' class on a dummy server.
	/// </summary>
	[TestFixture]
	public class BusStopTestsDummy
	{
		/// <summary>
		///     Directs traffic to live server.
		/// </summary>
		[OneTimeSetUp]
		public void Setup()
		{
			//Use the dummy server.
			ReadingBuses.SetDebuggingAsync(true);
		}

		/// <summary>
		///     Check the default constructor
		/// </summary>
		[Test]
		public void CheckDefaultConstructor()
		{
			const string actoCode = "039028160001";
			BusStop stop = new BusStop(actoCode);

			Assert.AreEqual(actoCode, stop.AtcoCode);
		}


		/// <summary>
		///     Checks that we can get archived time table data at the stop.
		/// </summary>
		[Test]
		public async Task GetArchivedTimeTableAsync()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			if ((await stop.GetArchivedTimeTable(DateTime.Now.AddDays(-1))).Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No time table data was retrieved.");
			}
		}


		/// <summary>
		///     Checks that an error is thrown if asking for data in the future.
		/// </summary>
		[Test]
		public void GetArchivedTimeTableErrorAsync()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			// Using a method as a delegate
			Assert.ThrowsAsync<ReadingBusesApiExceptionMalformedQuery>(async () =>
				await stop.GetArchivedTimeTable(DateTime.Now.AddDays(10)));
		}

		/// <summary>
		///     Checks that we can get archived time table data at the stop, filtered by service.
		/// </summary>
		[Test]
		public async Task GetArchivedTimeTableFilteredAsync()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			ArchivedBusTimeTable[] timeTableRecords =
				await stop.GetArchivedTimeTable(DateTime.Now.AddDays(-1), new BusService("17"));

			foreach (var record in timeTableRecords)
			{
				if (!record.GetService().ServiceId.Equals("17"))
				{
					Assert.Fail("Not all services matched the specified service ID.");
				}
			}


			if (timeTableRecords.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No time table data was retrieved when filtered.");
			}
		}

		/// <summary>
		///     Checks that we can get live data at a bus stop.
		/// </summary>
		[Test]
		[TestCase("039028150002")]
		[TestCase("039025980002")]
		public async Task GetLiveDataAsync(string actoCode)
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation(actoCode);

			if ((await stop.GetLiveData()).Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail(
					"No live data found at bus stop, this could be because there really is none. Make sure you run these tests at a reasonable British time.");
			}
		}


		/// <summary>
		///     Checks that we can get a bus stop point.
		/// </summary>
		[Test]
		public void GetPoint()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			if (!stop.GetPoint().Equals(null))
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("A null point was returned.");
			}
		}


		/// <summary>
		///     Checks that we can get an array of services at the stop.
		/// </summary>
		[Test]
		public void GetServicesAtStop()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");
			BusService[] services = stop.GetServices(Company.ReadingBuses);

			foreach (var service in services)
			{
				if (!service.OperatorCode.Equals(Company.ReadingBuses))
				{
					Assert.Fail("Not all services matched the specified operator.");
				}
			}

			if (stop.GetServices(Company.ReadingBuses).Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No bus services found at this bus stop.");
			}
		}


		/// <summary>
		///     Checks that we can get time table data at the stop.
		/// </summary>
		[Test]
		public async Task GetTimeTableAsync()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			if ((await stop.GetTimeTable(DateTime.Now)).Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No time table data was retrieved.");
			}
		}

		/// <summary>
		///     Checks that we can get time table data at the stop, filtered by service.
		/// </summary>
		[Test]
		public async Task GetTimeTableFilteredAsync()
		{
			BusStop stop = ReadingBuses.GetInstance().GetLocation("039025980002");

			BusTimeTable[] timeTableRecords = await stop.GetTimeTable(DateTime.Now, new BusService("17"));

			foreach (var record in timeTableRecords)
			{
				if (!record.GetService().ServiceId.Equals("17"))
				{
					Assert.Fail("Not all services matched the specified service ID.");
				}
			}


			if (timeTableRecords.Length != 0)
			{
				Assert.Pass();
			}
			else
			{
				Assert.Fail("No time table data was retrieved when filtered.");
			}
		}
	}
}
