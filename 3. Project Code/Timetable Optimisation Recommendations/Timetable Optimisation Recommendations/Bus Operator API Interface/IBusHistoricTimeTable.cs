// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;


namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// Used to store a historical time table record, which is an actual record for when a single bus arrive at a stop.
    /// </summary>
    public interface IBusHistoricTimeTable : IBusTimeTable
    {
        /// <value>The actual arrival time for the bus. </value>
        public DateTime? ActArrivalTime { get;  }

        /// <value>The actual departure time for the bus. </value>
        public DateTime? ActDepartureTime { get;  }

        /// <summary>
        /// Returns if the record could be made a "solid" record or not.
        /// A solid record is one with reported arrival and departure times. 
        /// </summary>
        /// <returns>true if Actual Arrival and Departure have values.</returns>
        public bool CouldBeSolid();

        /// <summary>
        /// Gets the solid representation of the same object.
        /// </summary>
        /// <returns>Gets the solid equivalence object.</returns>
        public IBusSolidHistoricTimeTable GetSolid();
    }
}
