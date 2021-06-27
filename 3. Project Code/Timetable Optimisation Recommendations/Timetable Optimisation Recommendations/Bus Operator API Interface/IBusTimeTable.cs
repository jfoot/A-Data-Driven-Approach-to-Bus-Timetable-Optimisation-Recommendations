// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// A class which represents a single time table record, this is a time and particular bus stop for one service.
    /// </summary>
    public interface IBusTimeTable
    {
        /// <value>The 'BusStop' object for the stop relating to the time table record..</value>
        public IBusStop Location { get; }

        /// <value>What number bus stop is this in the buses route, ie 1, is the first stop to visit.</value>
        public long Sequence { get; }

        /// <value>Is this bus heading inbound or outbound.</value>
        public Boolean IsOutbound { get; }

        /// <value>
        ///     A unique value that groups a selection of time table records across different bus stops to show one loop/ cycle
        ///     of a bus services route.
        /// </value>
        public string JourneyCode { get; }


        /// <value>
        /// A running board value, represents a group of journeys that one driver is expected to perform.
        /// These are therefore sequential services, driven using the same vehicle.
        /// </value>
        public string RunningBoard { get; }


        /// <value>Is this bus stop a timing point or not.</value>
        /// <remarks>
        ///     A timing point is a major bus stop, where the buses is expected to wait if its early and should actually arrive on
        ///     the scheduled time.
        ///     All non-timing points times are only estimated scheduled times. A timing point is much more accurate and strict
        ///     timings.
        ///     A stop which is a timing point for one service is not necessarily a timing point for another service, hence it is stored here
        ///     and not in the IBusStop. 
        /// </remarks>
        public bool IsTimingPoint { get; }

        /// <value>The scheduled arrival time for the bus. </value>
        public DateTime SchArrivalTime { get; }

        /// <value>The scheduled departure time for the bus. </value>
        public DateTime SchDepartureTime { get; }

        /// <summary>
        ///     Gets the related 'IBusService' object relating to the time table record.
        /// </summary>
        /// <returns>A 'BusService' object for this time table record.</returns>
        public IBusService Service { get; }

        /// <summary>
        /// A faster way to compare if two IBusTimeTable records are about the same bus stop.
        /// By simply comparing their string atco code, as opposed to finding and comparing the
        /// two bus stop objects.
        /// </summary>
        /// <param name="stop2">Another time table record to compare against.</param>
        /// <returns></returns>
        public bool WeakIsStopSame(IBusTimeTable stop2);

        /// <summary>
        /// Used to say that if given a bus stop object is the stop about this timetable record or not.
        /// </summary>
        /// <param name="stop2"></param>
        /// <returns></returns>
        public bool WeakIsStopSame(IBusStop stop2);


        /// <summary>
        /// Used to get a unique ID value to represent the timetable record.
        /// </summary>
        /// <returns>A value to represent the record.</returns>
        public string GetId();


        /// <summary>
        /// Used to check if the direction of travel of this record matches the value or not.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public bool MatchDirection(Direction direction);

    }
}
