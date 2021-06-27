// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Newtonsoft.Json;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusSolidHistoricTimeTable interface for the Reading Buses API.
    /// </summary>
    public class RbTimeTableSolidHistoric : RbTimeTable, IBusSolidHistoricTimeTable
    {
        [JsonProperty] public DateTime ActArrivalTime { get; private set; }
        [JsonProperty] public DateTime ActDepartureTime { get; private set; }


        /// <summary>
        /// Converts between a non-solid to solid object.
        /// </summary>
        /// <param name="inputTimeTable">The non-solid input record.</param>
        public static explicit operator RbTimeTableSolidHistoric(RbTimeTableHistoric inputTimeTable)
        {
            return new()
            {
                Location = inputTimeTable.Location,
                Sequence = inputTimeTable.Sequence,
                IsOutbound = inputTimeTable.IsOutbound,
                JourneyCode = inputTimeTable.JourneyCode,
                IsTimingPoint = inputTimeTable.IsTimingPoint,
                SchArrivalTime = inputTimeTable.SchArrivalTime,
                SchDepartureTime = inputTimeTable.SchDepartureTime,
                ActArrivalTime = inputTimeTable.ActArrivalTime ?? inputTimeTable.SchArrivalTime,
                ActDepartureTime = inputTimeTable.ActDepartureTime ?? inputTimeTable.SchDepartureTime,
                Service = inputTimeTable.Service,
                ServiceId = inputTimeTable.Service.ServiceId,
                AtcoCode = inputTimeTable.Location.AtcoCode,
                RunningBoard = inputTimeTable.JourneyCode
            };
        }
    }

}
