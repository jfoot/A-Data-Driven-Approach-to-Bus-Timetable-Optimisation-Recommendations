// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Collections.Generic;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Route_Analyser
{
    /// <summary>
    /// Route Segment is set of consecutive stops that two services share, the primary service,
    /// identified in the RouteSegmenetFinder and the secondary service that also shares it.
    /// </summary>
    public class RouteSegment
    {
        /// <summary>
        /// The other service that shares the route segment.
        /// </summary>
        public IBusService SecondaryService { get; }

        /// <summary>
        /// Defines the set of consecutive stops that makes up the route segment.
        /// </summary>
        public List<IBusStop> Stops { get; } = new();

        /// <summary>
        /// The internal constructor to create a route segment. This is done so you cannot make 
        /// a route segment object without the RouteSegmentFinder.
        /// </summary>
        /// <param name="service">The secondary service apart of the segment.</param>
        /// <param name="stop">The initial/first stop in the segment.</param>
        internal RouteSegment(IBusService service, IBusStop stop)
        {
            SecondaryService = service;
            AddStop(stop);
        }

        /// <summary>
        /// The internal constructor to create a route segment. This is done so you cannot make 
        /// a route segment object without the RouteSegmentFinder.
        /// </summary>
        /// <param name="service">The secondary service apart of the segment.</param>
        /// <param name="stops">The initial stops in the segment.</param>
        internal RouteSegment(IBusService service, List<IBusStop> stops)
        {
            SecondaryService = service;
            Stops.AddRange(stops);
        }


        /// <summary>
        /// Finds the length of the segment of stops.
        /// </summary>
        /// <returns>The length of the segment.</returns>
        public int LengthOfSegment()
        {
            return Stops.Count;
        }

        /// <summary>
        /// Adds a new stop onto the segment.
        /// </summary>
        /// <param name="stop">the stop to add to the segment.</param>
        private void AddStop(IBusStop stop)
        {
            Stops.Add(stop);
        }

    }
}
