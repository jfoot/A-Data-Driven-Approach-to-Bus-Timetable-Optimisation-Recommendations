// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Threading.Tasks;


namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// A class which represents a single bus stop.
    /// </summary>
    public interface IBusStop
    {
        /// <value>The unique identifier for a bus stop.</value>
        public string AtcoCode { get; }

        /// <value>The public, easy to understand stop name.</value>
        public string CommonName { get; }

        /// <value>The latitude of the bus stop</value>
        public string Latitude { get; }

        /// <value>The longitude of the bus stop</value>
        public string Longitude { get;  }

        /// <value>The bearing of the bus stop</value>
        public string Bearing { get; }

        /// <value>A list of the IDs of the services which stop at this stop.</value>
        public string[]? Services { get; }


        /// <summary>
        ///     Finds the 'BusService' object for all of the bus services which visit this stop.
        /// </summary>
        /// <returns>A list of BusService Objects for services which visit this bus stop.</returns>
        public IBusService[] GetServices();


        /// <summary>
        ///     Gets the archived real bus departure and arrival times along with their time table history at this specific bus
        ///     stop.
        /// </summary>
        /// <param name="date">The date you want time table data for. This should be a date in the past.</param>
        /// <returns></returns>
        public Task<IBusHistoricTimeTable[]?> GetArchivedTimeTable(DateTime date);

        /// <summary>
        /// Tells you if a file has been cached or not on disk.
        /// </summary>
        /// <param name="date">The date for the time table date to search for.</param>
        /// <returns>True if the data is cached on disk</returns>
        /// <see cref="GetArchivedTimeTable(DateTime)"/>        
        public bool IsArchivedTimeTableCached(DateTime date);


        /// <summary>
        /// Get "Weak" archived timetable doesn't actually call-upon the API feed.
        /// It will look at the cached data on disk, finding services that visit the stop,
        /// ask for all their timetables and then filter out all the records that are not about this stop.
        /// This means that you might end up missing out on some data if you've not cached the service.
        /// However it will be significantly faster than actually calling upon the API feed. 
        /// </summary>
        /// <param name="date">The date to get stop timetable data from.</param>
        /// <returns>Timetable data for the stop, made up of any cache data about it.</returns>
        public Task<IBusHistoricTimeTable[]?> GetWeakArchivedTimeTable(DateTime date);

    }
}
