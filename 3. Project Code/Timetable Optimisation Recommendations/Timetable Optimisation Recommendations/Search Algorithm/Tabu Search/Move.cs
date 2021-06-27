// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search
{
    /// <summary>
    /// Used to represent a single move of the search algorithm, this involves making a change to
    /// one services timetable, on one running-board. A single timetable record is moved and then
    /// the surrounding records will also need to be edited in forwards and backwards propagation.
    ///
    /// As such a move is represented as the service it is about and an array of blamed timetable records,
    /// which contain the new timetable for the service. Most records won't have actually moved.
    /// </summary>
    public struct Move
    {
        ///<value>States what service this moves alters.</value>
        public IBusService Service { get; init; }
        ///<value>The new timetable for the service after the move has been applied.</value>
        public BlamedBusTimeTable[] TimeTable { get; init; }
        ///<value>The IDs of the records in the timetable that have actually changed. </value>
        public List<string> ChangedRecordsIDs { get; init; } 
        ///<value>The total amount of minuets changes in the move from the initial solution.</value>
        public double ChangeAmount { get; init; }

        ///<value>The new proposed scheduled arrival time.</value>
        public DateTime ProposedSchArrivalTime { get; init; }
        ///<value>The new proposed scheduled departure time.</value>
        public DateTime ProposedSchDepartureTime { get; init; }
        ///<value>The timetable record highlighted as being the problem.</value>
        public BlamedBusTimeTable TargetRecord { get; init; }


        /// <summary>
        /// Provides a string representation of the move. This is only accurate if TargetRecord.SetSuggestedToReal() hasn't already been called.
        /// </summary>
        /// <returns>A string representation of the changes.</returns>
        public override string ToString()
        {
            return "Service " + Service.ServiceId + (TargetRecord.IsOutbound ? " Outbound" : " Inbound") + " at Stop '" + TargetRecord.Location.CommonName + " (" + TargetRecord.Location.AtcoCode + ")', Originally Scheduled for: "
                   + TargetRecord.SchArrivalTime.ToString("HH:mm") + ", now moved to: " + TargetRecord.ProposedSchArrivalTime().ToString("HH:mm") + " - Journey ID " + TargetRecord.JourneyCode;
        }
    }
}
