// Copyright (c) Joanthan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Route_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;
using TOR_Tests.Stubs;

namespace TOR_Tests.Tests.Timetable_Evaluator
{
    class ServiceCohesionEvaluatorTests
    {
       
        [Test]
        public void PerfectTimeTableTest()
        {
            DateTime[] dates = {
                new (2020, 01, 01)
            };

            RouteSegmentCollection collection = new RouteSegmentCollection();
            IBusStop stop = new StopStub("123", new[] { "1", "2", "3" });

            IBusService ser1 = new ServiceStub("1");
            IBusService ser2 = new ServiceStub("2");
            IBusService ser3 = new ServiceStub("3");

            collection.ServicesAtStopOfInterest.Add(stop, new List<IBusService>() { ser1, ser2, ser3 });

            Dictionary<IBusService, IBusTimeTable[]> busTimeTables = new();
            busTimeTables.Add(ser1, new[]{
                new TimeTableStub(stop, new DateTime(2020,01,01,01,0,0), new DateTime(2020, 01, 01, 01, 1, 0), ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,01,30,0), new DateTime(2020, 01, 01, 01, 31, 0),ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,0,0), new DateTime(2020, 01, 01, 02, 1, 0),ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,30,0), new DateTime(2020, 01, 01, 02, 31, 0),ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,03,0,0), new DateTime(2020, 01, 01, 03, 1, 0),ser1)
            });

            busTimeTables.Add(ser2, new[]{
                new TimeTableStub(stop, new DateTime(2020,01,01,01,10,0), new DateTime(2020, 01, 01, 01, 11, 0),ser2),
                new TimeTableStub(stop, new DateTime(2020,01,01,01,40,0), new DateTime(2020, 01, 01, 01, 41, 0),ser2),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,10,0), new DateTime(2020, 01, 01, 02, 11, 0),ser2),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,40,0), new DateTime(2020, 01, 01, 02, 41, 0),ser2),
                new TimeTableStub(stop, new DateTime(2020,01,01,03,10,0), new DateTime(2020, 01, 01, 03, 11, 0),ser2)
            });

            busTimeTables.Add(ser3, new[]{
                new TimeTableStub(stop, new DateTime(2020,01,01,01,20,0), new DateTime(2020, 01, 01, 01, 21, 0),ser3),
                new TimeTableStub(stop, new DateTime(2020,01,01,01,50,0), new DateTime(2020, 01, 01, 01, 51, 0),ser3),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,20,0), new DateTime(2020, 01, 01, 02, 21, 0),ser3),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,50,0), new DateTime(2020, 01, 01, 02, 51, 0),ser3),
                new TimeTableStub(stop, new DateTime(2020,01,01,03,20,0), new DateTime(2020, 01, 01, 03, 21, 0),ser3)
            });


            TimeTableEvaluator evaluator = new TimeTableEvaluator(dates, collection, busTimeTables);
            ServiceCohesionEvaluator cohesionEvaluator = new ServiceCohesionEvaluator(evaluator);


            cohesionEvaluator.FindBlameServiceCohesion(evaluator.CurrentSolution);

            foreach ((IBusService _, BlamedBusTimeTable[] records) in evaluator.CurrentSolution.BusTimeTables)
                foreach (var record in records)
                    if(record.CohesionWeights.Weight != null)
                        Assert.AreEqual(0, record.CohesionWeights.Weight);
        }


        [Test]
        public void OffTimeTableTest()
        {
            DateTime[] dates = {
                new(2020, 01, 01)
            };

            RouteSegmentCollection collection = new RouteSegmentCollection();
            IBusStop stop = new StopStub("123", new[] { "1", "2" });

            IBusService ser1 = new ServiceStub("1");
            IBusService ser2 = new ServiceStub("2");


            collection.ServicesAtStopOfInterest.Add(stop, new List<IBusService>() { ser1, ser2 });

            Dictionary<IBusService, IBusTimeTable[]> busTimeTables = new();
            busTimeTables.Add(ser1, new[]{
                new TimeTableStub(stop, new DateTime(2020,01,01,01,0,0), new DateTime(2020, 01, 01, 01, 1, 0), ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,01,30,0), new DateTime(2020, 01, 01, 01, 31, 0),ser1),
                new TimeTableStub(stop, new DateTime(2020,01,01,02,0,0), new DateTime(2020, 01, 01, 02, 1, 0),ser1)
            });

            busTimeTables.Add(ser2, new[]{
                new TimeTableStub(stop, new DateTime(2020,01,01,01,15,0), new DateTime(2020, 01, 01, 01, 11, 0),ser2),
                new TimeTableStub(stop, new DateTime(2020,01,01,01,40,0), new DateTime(2020, 01, 01, 01, 41, 0),ser2)
            });


            TimeTableEvaluator evaluator = new TimeTableEvaluator(dates, collection, busTimeTables);
            ServiceCohesionEvaluator cohesionEvaluator = new ServiceCohesionEvaluator(evaluator);
            
            cohesionEvaluator.FindBlameServiceCohesion(evaluator.CurrentSolution);


            //Flattens the 2D array into one, gets all timetable records of all services.
            BlamedBusTimeTable[] resultsFlattened = evaluator.CurrentSolution.BusTimeTables.Values.SelectMany(x => x).OrderBy(r => r.SchArrivalTime).ToArray();
           

            Assert.AreEqual(0, resultsFlattened[0].CohesionWeights.Weight);
            Assert.AreEqual(0, resultsFlattened[1].CohesionWeights.Weight);
            Assert.AreEqual(0, resultsFlattened[2].CohesionWeights.Weight);
            Assert.AreEqual(0.5, resultsFlattened[3].CohesionWeights.Weight);
            Assert.AreEqual(1, resultsFlattened[4].CohesionWeights.Weight);
        }

    }
}
