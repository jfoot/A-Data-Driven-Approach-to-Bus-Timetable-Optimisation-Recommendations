// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Analyser
{
    /// <summary>
    /// A cluster is a selection of dates which all have the same timetable.
    /// A group is a span of consecutive days within the cluster, that all have this same timetable.
    /// </summary>
    public class Cluster
    {
        /// <summary>
        /// A list of dates which are associated with this timetable, i.e they had the same timetable as each other. 
        /// </summary>
        public List<DateTime> AssociatedTimes { get; }
        
        /// <summary>
        /// The timetable for this cluster, an array of records for one day. 
        /// </summary>
        public IBusTimeTable[] BusTimeTables { get; }


        /// <summary>
        /// The Grouping associated with the cluster.
        /// </summary>
        public Group? GroupingAssociated { get; private set; }

       
        /// <summary>
        /// A unique ID for the cluster.
        /// </summary>
        public int ClusterId { get; }

        /// <summary>
        /// Counts how many clusters have been generated, this is so each cluster can have its own ID.
        /// Volatile so that if threaded it should remain consistent.
        /// </summary>
        private static volatile int _clusterCount = 0;

        /// <summary>
        /// The default constructor for the cluster. Given one date for each the timetable applies
        /// and then the timetable associated.
        /// </summary>
        /// <param name="date">A date associated with the timetable.</param>
        /// <param name="timeTable">The timetable it self.</param>
        public Cluster(DateTime date, IBusTimeTable[] timeTable)
        {
            AssociatedTimes = new List<DateTime> { date };
            BusTimeTables = timeTable;
            ClusterId = _clusterCount++;
        }

        /// <summary>
        /// Used to associate another date with the cluster.
        /// </summary>
        /// <param name="date">A new date to add to the cluster.</param>
        public void AddDate(DateTime date)
        {
            AssociatedTimes.Add(date);
        }

        /// <summary>
        /// Builds up the groups from the cluster.
        /// </summary>
        public void CalculateGrouping()
        {
            GroupingAssociated = new Group(AssociatedTimes);
        }
       

        /// <summary>
        /// Gets the associated service for the cluster.
        /// </summary>
        /// <returns></returns>
        public IBusService GetAssociatedService()
        {
            if (BusTimeTables.Length != 0)
                return BusTimeTables[0].Service;

            //This should be impossible to be able to get here.
            throw new Exception("There are no bus services in this cluster.");
        }

    }
}
