// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Newtonsoft.Json;
using ReadingBusesAPI.TimeTable;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusHistoricTimetable interface for the Reading Buses API.
    /// </summary>
    public class RbTimeTableHistoric : RbTimeTable, IBusHistoricTimeTable
    {
        [JsonProperty] public DateTime? ActArrivalTime { get; private set; }
        [JsonProperty] public DateTime? ActDepartureTime { get; private set; }

        public bool CouldBeSolid()
        {
            return ActArrivalTime.HasValue && ActDepartureTime.HasValue;
        }

        public IBusSolidHistoricTimeTable GetSolid()
        {
            return (RbTimeTableSolidHistoric)this;
        }

        public static explicit operator RbTimeTableHistoric(ArchivedBusTimeTable inputTimeTable)
        {
            return new()
            {
                Location = (RbBusStop)inputTimeTable.Location,
                Sequence = inputTimeTable.Sequence,
                IsOutbound = inputTimeTable.Direction.Equals(Direction.Outbound),
                JourneyCode = inputTimeTable.JourneyCode,
                IsTimingPoint = inputTimeTable.IsTimingPoint,
                SchArrivalTime = inputTimeTable.SchArrivalTime,
                SchDepartureTime = inputTimeTable.SchDepartureTime,
                ActArrivalTime = inputTimeTable.ActArrivalTime,
                ActDepartureTime = inputTimeTable.ActDepartureTime,
                Service = (RbBusService)inputTimeTable.GetService(),
                ServiceId = inputTimeTable.GetService().ServiceId,
                AtcoCode = inputTimeTable.Location.AtcoCode,
                RunningBoard = inputTimeTable.JourneyCode
            };
        }
    }

}
