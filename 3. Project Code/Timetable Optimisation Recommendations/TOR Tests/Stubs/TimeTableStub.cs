// Copyright (c) Joanthan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0

using System;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace TOR_Tests.Stubs
{
    public class TimeTableStub : IBusTimeTable
    {
        public IBusStop Location { get; set; }
        public long Sequence { get; set; }
        public bool IsOutbound { get; set; }
        public string JourneyCode { get; set; }
        public string RunningBoard { get; set; }
        public bool IsTimingPoint { get; set; }
        public DateTime SchArrivalTime { get; set; }
        public DateTime SchDepartureTime { get; set; }
        public IBusService Service { get; set; }

        public TimeTableStub(IBusStop stop, DateTime schArrivalTime, DateTime schDepartureTime, IBusService service)
        {
            Location = stop;
            SchArrivalTime = schArrivalTime;
            SchDepartureTime = schDepartureTime;
            Service = service;
        }

        public bool WeakIsStopSame(IBusTimeTable stop2)
        {
            return stop2.Location.AtcoCode == Location.AtcoCode;
        }

        public bool WeakIsStopSame(IBusStop stop2)
        {
            return stop2.AtcoCode == Location.AtcoCode;
        }

        public string GetId()
        {
            throw new NotImplementedException();
        }

        public bool MatchDirection(Direction direction)
        {
            throw new NotImplementedException();
        }
    }
}
