// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Performance_Evaluator
{
    /// <summary>
    /// Used to represent the lateness of one single record.
    /// </summary>
    public class LatenessRecord
    {
        ///<value>How late the service was</value>
        public double Lateness { get; }
        ///<value>The time of day it was meant to have arrived, to see if lateness changes throughout the day.</value>
        public DateTime SchArrivalTime { get;  }

        /// <summary>
        /// Returns true if late, which is earlier than one min or later than five min.
        /// </summary>
        public bool IsLate
        {
            get
            {
                return Lateness >= 5 || Lateness <= -1;
            }
        }

        /// <summary>
        /// The default constructor for the class, takes in a historical record and creates a lateness record out of it.
        /// </summary>
        /// <param name="record">The historical timetable record representing it.</param>
        public LatenessRecord(IBusHistoricTimeTable record)
        {
            Lateness = ((record.ActArrivalTime ?? record.SchArrivalTime) - record.SchArrivalTime).TotalMinutes;
            SchArrivalTime = record.SchArrivalTime;
        }

    }
}
