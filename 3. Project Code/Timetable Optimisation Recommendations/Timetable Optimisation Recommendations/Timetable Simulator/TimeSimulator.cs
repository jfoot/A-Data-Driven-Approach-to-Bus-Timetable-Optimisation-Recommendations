// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;

namespace Timetable_Optimisation_Recommendations.Timetable_Simulator
{
    /// <summary>
    /// Shared common code used between both the Journey Time Simulator and the Dwell Time Simulator.
    /// Manila the weighted averaging code and accuracy measures. 
    /// </summary>
    public abstract class TimeSimulator
    {
        /// <summary>
        /// Calculates the inverse of a value, but if the value is less than one just return one.
        /// </summary>
        /// <param name="value1">Value to inverse</param>
        /// <returns>Inverses the value, unless less than one, then return one.</returns>
        protected static double CalculateInverseWeight(double value1)
        {
            return value1 <= 1 ? 1.0 : 1.0 / value1;
        }


        /// <summary>
        /// Calculates the inverse of the smallest of the two values, but if the smallest value is less than one, just return one.
        /// </summary>
        /// <param name="value1">Value one</param>
        /// <param name="value2">Value two</param>
        /// <returns>The inverse of the smallest of the two values, unless less than one, then return one.</returns>
        protected static double CalculateInverseWeight(double value1, double value2)
        {
            double minVal = Math.Min(value1, value2);
            return minVal <= 1 ? 1.0 : 1.0 / minVal;
        }


        /// <summary>
        /// Given a list of estimated durations and their estimated accuracy/ confidence level generate the weighted average of all the values.
        /// </summary>
        /// <param name="times">A list of tuples of estimated durations and accuracy</param>
        /// <returns>The new single weighted average of all the values.</returns>
        protected static TimeSpan GenerateWeightedAverage(List<(TimeSpan duration, double accuracyWeight)> times)
        {
            //If there are no values, return 0.
            if (times.Count == 0)
                return new TimeSpan(0);

            //Stores the weighted values
            List<TimeSpan> weightedTemps = new();
            //Store the total value of all weights.
            double totalWeight = 0;

            //Goes for every tuple in the times and generates the weighted value.
            foreach ((TimeSpan duration, double accuracyWeight) in times)
            {
                try
                {
                    weightedTemps.Add(duration * accuracyWeight);
                    totalWeight += accuracyWeight;
                }
                catch (Exception)
                {
                    Console.WriteLine("ERRORR : " + duration + " WEIGHTING " + accuracyWeight);
                }
            }

            //Sum up all the values and then divide by the total number of weights. 
            return new TimeSpan((long)(weightedTemps.Sum(v => v.Ticks) / totalWeight));
        }
    }
}
