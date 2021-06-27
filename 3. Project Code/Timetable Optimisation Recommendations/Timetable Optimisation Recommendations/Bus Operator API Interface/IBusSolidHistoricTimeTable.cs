// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;


namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// Used to store a historical time table record, which is an actual record for when a single bus arrive at a stop.
    /// Solid, contains only records that had actual values recorded. Any non-solid record does not necessarily contain values.
    /// </summary>
    public interface IBusSolidHistoricTimeTable : IBusTimeTable
    {
        /// <value>The actual arrival time for the bus. </value>
        public DateTime ActArrivalTime { get;  }

        /// <value>The actual departure time for the bus. </value>
        public DateTime ActDepartureTime { get;  }

    }
}
