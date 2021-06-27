// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Newtonsoft.Json;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// Used when you need to problematically make a new IBusTimetable record,
    /// for example when you are making a new timetable during the evaluator. 
    /// </summary>
    public class BusTimeTableStub : IBusTimeTable
    {

        [JsonProperty]
        public long Sequence { get; protected set; }
        [JsonProperty]
        public bool IsOutbound { get; protected set; }
        [JsonProperty]
        public string JourneyCode { get; protected set; }
        [JsonProperty]
        public bool IsTimingPoint { get; protected set; }
        [JsonProperty]
        public DateTime SchArrivalTime { get; set; }
        [JsonProperty]
        public DateTime SchDepartureTime { get; set; }
        [JsonProperty]
        public string ServiceId { get; set; }
        [JsonProperty]
        public string StopId { get; set; }

        [JsonProperty]
        public string RunningBoard { get; set; }

        public IBusStop Location => BusOperatorFactory.Instance.Operator.GetLocation(StopId);
        

        public IBusService Service => BusOperatorFactory.Instance.Operator.GetService(ServiceId);

        public BusTimeTableStub(IBusTimeTable timeTable)
        {
            StopId = timeTable.Location.AtcoCode;
            Sequence = timeTable.Sequence;
            IsOutbound = timeTable.IsOutbound;
            JourneyCode = timeTable.JourneyCode;
            IsTimingPoint = timeTable.IsTimingPoint;
            ServiceId = timeTable.Service.ServiceId;
            SchArrivalTime = timeTable.SchArrivalTime;
            SchDepartureTime = timeTable.SchDepartureTime;
            RunningBoard = timeTable.RunningBoard;
        }


        public bool MatchDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Inbound => !IsOutbound,
                Direction.Outbound => IsOutbound,
                _ => true
            };
        }


        public BusTimeTableStub(IBusTimeTable timeTable, TimeSpan schArrivalTime, TimeSpan schDepartureTime) : this(timeTable)
        {
            SchArrivalTime = SchArrivalTime.Date + schArrivalTime;
            SchDepartureTime = SchDepartureTime.Date  + schDepartureTime;
        }


        public bool WeakIsStopSame(IBusTimeTable stop2)
        {
            return StopId == stop2.Location.AtcoCode;
        }

        public bool WeakIsStopSame(IBusStop stop2)
        {
            return StopId == stop2.AtcoCode;
        }

        public string GetId()
        {
            return ServiceId + StopId + JourneyCode + Sequence;
        }
    }
}
