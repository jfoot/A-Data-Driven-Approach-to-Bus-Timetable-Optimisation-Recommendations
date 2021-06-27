// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Performance_Evaluator
{
    /// <summary>
    /// A class used to generate the performance of the historical/current timetables. 
    /// </summary>
    public class PerformanceEvaluator
    {
        ///<value>Contains a dictionary of bus services along with a list of lateness records, one for each historical timetable record.</value>
        private readonly ConcurrentDictionary<IBusService, List<LatenessRecord>> _serviceLateness = new();

        ///<value>The public output/result of the evaluator a lateness report summaries the list of lateness records for each service.</value>
        public List<LatenessReport> ServiceLatenessReports { get; private set; } = new();

        /// <summary>
        /// Used to add onto a services lateness record.
        /// </summary>
        /// <param name="service">The service you are adding to.</param>
        /// <param name="records">The records to convert and add.</param>
        public void AddRecords(IBusService service, IBusHistoricTimeTable[]? records)
        {
            if (records == null)
                return;

            //If this is the first time the service is being added, add it to the dictionary. 
            if(!_serviceLateness.ContainsKey(service))
                _serviceLateness.TryAdd(service, new List<LatenessRecord>(records.Length));

            //For each record convert it and add it.
            foreach (IBusHistoricTimeTable record in records)
                _serviceLateness[service].Add(new LatenessRecord(record));
        }

        /// <summary>
        /// Generates the summarized report for each service, this is what is being displayed to the end user.
        /// </summary>
        public void GenerateLatenessReport()
        {
            ServiceLatenessReports = new(_serviceLateness.Count);

            //Goes through every service and creates a new report for it.
            foreach ((IBusService busService, List<LatenessRecord> values) in _serviceLateness)
            {
                ServiceLatenessReports.Add(new LatenessReport()
                {
                    Service =  busService,
                    OnTimePercentage = CalculateOnTimePercentage(values),
                    AvgLatenessString = AverageLateness(values)
                });
            }
        }

        /// <summary>
        /// Given a list of lateness records calculate the percentage of services on time.
        /// </summary>
        /// <param name="values">A list of lateness records</param>
        /// <returns>The on time percentage</returns>
        private static double CalculateOnTimePercentage(List<LatenessRecord> values)
        {
            if (values.Count == 0)
                return 0.0;

            return values.Count(val => !val.IsLate) / (double)values.Count;
        }

        /// <summary>
        /// Given a list of lateness records calculate the average lateness.
        /// </summary>
        /// <param name="values">A list of lateness records.</param>
        /// <returns>The average lateness for the service.</returns>
        private static string AverageLateness(List<LatenessRecord> values)
        {
            if (values.Count == 0)
                return "0 min";

            return values.Average(val => val.Lateness).ToString("0.0 min");
        }

    }
}
