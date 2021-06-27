// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Performance_Evaluator
{
     /// <summary>
     /// Used for MVVM to bind to the GUI, this contains data summerising the performance of a service.
     /// </summary>
    public struct LatenessReport
    {
        ///<value>The Service the report pertains to.</value>
        public IBusService Service { get; init; }
        ///<value>The On Time Percentage as a double.</value>
        public double OnTimePercentage { get; init; }
        ///<value>The average lateness of a service in min.</value>
        public string AvgLatenessString { get; init; }

        ///<value>The on time percentage formatted nicely as a string value.</value>
        public string OnTimePercentageString
        {
            get
            {
                return OnTimePercentage.ToString("00.0%");
            }
        }
    }
}
