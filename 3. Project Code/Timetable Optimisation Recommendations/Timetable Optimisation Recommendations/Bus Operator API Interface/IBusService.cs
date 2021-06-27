// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Threading.Tasks;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// used to state the direction of travel of a service.
    /// </summary>
    public enum Direction
    {
        Inbound,
        Outbound,
        Both
    }

    /// <summary>
    /// A class which represents a single bus service.
    /// </summary>
    public interface IBusService
    {
        /// <value>
        ///     The unique alphanumeric identifier for a bus service.
        /// </value>
        public string ServiceId { get; }


        /// <summary>
        ///     Gets an array of 'BusStop' objects the bus service travels too as an array of BusStop objects.
        ///     If the API is invalid and links to a Bus Stop not in the list of locations it will simply be ignored.
        /// </summary>
        /// <param name="direction">Used to filter by the direction of travel the stops are on.</param>
        /// <returns>An array of BusStop objects for the stops visited by this service.</returns>
        /// <remarks>
        ///     It is assumed that the ordering of the array is the ordering in which a service will visit all of the stops.
        ///     If the ordering is incorrect the route-segment finder will fail.
        /// </remarks>
        public Task<IBusStop[]> GetLocations(Direction direction = Direction.Both);

        
        /// <summary>
        ///     Gets the planned timetable departure and arrival times for this service on a specific date.
        /// </summary>
        /// <param name="date">the date on which you want a archived timetable data for. This should be a date in the past.</param>
        /// <returns>An array of time table records, containing the planned scheduled and actual arrival and departure times of buses. </returns>
        public Task<IBusTimeTable[]?> GetTimeTable(DateTime date);


        /// <summary>
        /// Tells you if a file has been cached or not on disk.
        /// </summary>
        /// <param name="date">The date for the time table date to search for.</param>
        /// <returns>True if the data is cached on disk</returns>
        /// <see cref="GetTimeTable(DateTime)"/>
        public bool IsTimeTableCached(DateTime date);


        /// <summary>
        ///     Gets the archived real bus departure and arrival times along with their time table history for this service on a
        ///     specific date.
        /// </summary>
        /// <param name="date">the date on which you want a archived timetable data for. This should be a date in the past.</param>
        /// <returns>An array of time table records, containing the scheduled and actual arrival and departure times of buses. </returns>
        public Task<IBusHistoricTimeTable[]?> GetArchivedTimeTable(DateTime date);


        /// <summary>
        /// Tells you if a file has been cached or not on disk.
        /// </summary>
        /// <param name="date">The date for the time table date to search for.</param>
        /// <returns>True if the data is cached on disk</returns>
        /// <see cref="GetArchivedTimeTable(DateTime)"/>
        public bool IsArchivedTimeTableCached(DateTime date);


        /// <summary>
        /// Given another IBusService Object, check if it is the same service or not.
        /// Only by comparing the service ID value.
        /// </summary>
        /// <param name="service">The other service you wish to compare against.</param>
        /// <returns></returns>
        public bool IsWeakServiceSame(IBusService service);
    }
}
