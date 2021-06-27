// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Simulator
{

    /// <summary>
    /// A simplistic class used to help represent the journey time between two stops.
    /// </summary>
    public class JourneyTime
    {
        ///<value>The time it takes to travel between the two stops.</value>
        public TimeSpan TravelTime { get; }
        ///<value>
        /// The time of departure at r1 (used for forwards propagation) or time of arrival at r2 (used for backward propagation).
        /// Dependent upon the input argument to the constructor.
        /// </value>
        public TimeSpan TimeOfInterest { get; }


        /// <summary>
        /// The default journey time constructor, takes in two solid historical records
        /// and works out key metrics. It is important the two stops are in order,
        /// r1 is the first stop and r2 is the next stop.
        /// </summary>
        /// <param name="r1">Timetable record one.</param>
        /// <param name="r2">Timetable record two.</param>
        /// <param name="isForwardProp">Is forward propagation active, else using backwards.</param>
        public JourneyTime(IBusSolidHistoricTimeTable r1, IBusSolidHistoricTimeTable r2, bool isForwardProp)
        {
            TravelTime = (r2.ActArrivalTime - r1.ActDepartureTime);
            TimeOfInterest = isForwardProp ? r1.ActDepartureTime.TimeOfDay : r2.ActArrivalTime.TimeOfDay;
        }
    }
}
