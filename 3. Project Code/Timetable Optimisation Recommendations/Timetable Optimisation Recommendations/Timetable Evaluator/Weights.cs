// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{

    /// <summary>
    /// The weights class is used to store a target arrival and departure time, along with a raw and standardised weight for how much it should pull towards it.
    /// </summary>
    public class Weights : ICloneable
    {

        ///<value>The overall weight. This might be standardised, but will always be an absolute value.</value>
        public double? Weight { get; set; } = null;

        ///<value>
        /// The raw non-standardised and non-absolute weight used between iterations to regenerate the new standardised weight if possible.
        /// This is used mainly for speed and efficiency purposes rather than anything else.
        /// </value>
        public double? RawWeight { get; set; }

        ///<value>The Arrival Time value the weight is pulling towards.</value>
        public DateTime TargetSchArrivalTime { get; set; }

        ///<value>The Departure Time value the weight is pulling towards.</value>
        public DateTime TargetSchDepartureTime { get; set; }

        /// <summary>
        /// Performs a shallow copy of the weights object.
        /// But deep-copy would do the same as there are no object references in this class.
        /// </summary>
        /// <returns>A copy of the object.</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
