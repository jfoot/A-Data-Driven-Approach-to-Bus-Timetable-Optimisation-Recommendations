// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Newtonsoft.Json;
using ReadingBusesAPI.TimeTable;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusTimetable interface for the Reading Buses API.
    /// </summary>
    public class RbTimeTable : IBusTimeTable
    {
        [JsonProperty] public long Sequence { get; protected set; }
        [JsonProperty] public bool IsOutbound { get; protected set; }
        [JsonProperty] public string JourneyCode { get; protected set; } = string.Empty;
        [JsonProperty] public bool IsTimingPoint { get; protected set; }
        [JsonProperty] public DateTime SchArrivalTime { get; protected set; }
        [JsonProperty] public DateTime SchDepartureTime { get; protected set; }
        [JsonProperty] public string RunningBoard { get; protected set; } = string.Empty;


        [JsonProperty] protected string ServiceId = string.Empty;
        [JsonIgnore] private IBusService? _service;
        [JsonIgnore]
        public IBusService Service
        {
            get => _service ??= RbBusOperator.GetInstance().GetService(ServiceId);
            protected set => _service = value;
        }


        [JsonProperty] protected string AtcoCode = string.Empty;
        [JsonIgnore] private IBusStop? _location;

        [JsonIgnore]
        public IBusStop Location
        {
            get => _location ??= RbBusOperator.GetInstance().GetLocation(AtcoCode);
            protected set => _location = value;
        }


        public static explicit operator RbTimeTable(BusTimeTable inputTimeTable)
        {
            return new()
            {
                Location = (RbBusStop)inputTimeTable.Location,
                Sequence = inputTimeTable.Sequence,
                IsOutbound = inputTimeTable.Direction.Equals(ReadingBusesAPI.Common.Direction.Outbound),
                JourneyCode = inputTimeTable.JourneyCode,
                IsTimingPoint = inputTimeTable.IsTimingPoint,
                SchArrivalTime = inputTimeTable.SchArrivalTime,
                SchDepartureTime = inputTimeTable.SchDepartureTime,
                Service = (RbBusService)inputTimeTable.GetService(),
                ServiceId = inputTimeTable.GetService().ServiceId,
                AtcoCode = inputTimeTable.Location.AtcoCode,
                RunningBoard = inputTimeTable.JourneyCode
            };
        }

        public bool WeakIsStopSame(IBusTimeTable stop2)
        {
            RbTimeTable stop2Rb = (stop2 as RbTimeTable) ?? throw new AggregateException("You can only compare a Reading Buses Timetable to a Reading Buses Timetable.");
            return AtcoCode == stop2Rb.AtcoCode;
        }

        public string GetId()
        {
            return ServiceId + AtcoCode + JourneyCode + Sequence + RunningBoard;
        }

        public bool WeakIsStopSame(IBusStop stop2)
        {
            RbBusStop stop2Rb = (stop2 as RbBusStop) ?? throw new AggregateException("You can only compare a Reading Buses Stop to a Reading Buses Timetable.");
            return AtcoCode == stop2Rb.AtcoCode;
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
    }
}
