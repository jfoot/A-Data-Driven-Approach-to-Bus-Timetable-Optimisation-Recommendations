// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Request_Manager;

namespace Timetable_Optimisation_Recommendations.Timetable_Analyser
{
    /// <summary>
    /// The grouper class can be used to help find patterns in the timetable where several days shared the same timetable.
    /// </summary>
    public class TimeTableGrouper
    {

        /// <summary>
        /// The timetable for which you wish to group it's timetables for.
        /// </summary>
        private readonly IBusService _service;

        /// <summary>
        /// The default constructor for the grouper.
        /// </summary>
        /// <param name="service">The service for which you wish to find groupings.</param>
        public TimeTableGrouper(IBusService service)
        {
            _service = service;
        }

        /// <summary>
        /// Finds an array of clusters, within the timetable data between two dates.
        /// If every day had a new timetable then you would have one cluster per day.
        /// </summary>
        /// <param name="progress">The progress for how far along it is.</param>
        /// <param name="startDate">The start date to find a group in.</param>
        /// <param name="endDate">The end date to find a group in.</param>
        /// <returns>An array of found groups.</returns>
        /// <remarks>
        /// This does currently NOT work for a service which operates over night and might have a different start and end day.
        /// This is assuming that the start and end day is on the same day.
        /// </remarks>
        public async Task<Cluster[]> FindGroupings(IProgress<ProgressReporting> progress, DateTime startDate, DateTime endDate)
        {
            //Get the timetable data for the service.
            IBusTimeTable[][] data = await TimetableRetrieval.GetTimeTableBatch(startDate, endDate, _service, progress);
            //Stores a list of found clusters, one cluster is one timetable.
            List<Cluster> clusters = new();

            await Task.Run(() =>
            {
                //For each day in the timetable data.
                for (int i = 0; i < data.Length; i++)
                {
                    IBusTimeTable[] day = data[i];
                    //Check does this cluster timetable already exists/ is it a known cluster.
                    Cluster? cluster = clusters.FirstOrDefault(x => IsSameSetofRecords(x.BusTimeTables, day));
                    //If so add this date to the cluster.
                    if (cluster != null)
                        cluster.AddDate(day[0].SchArrivalTime.Date);
                    //Else it is a new cluster so create one.
                    else
                        clusters.Add(new Cluster(day[0].SchArrivalTime.Date, day));

                    progress.Report(new ProgressReporting((i + 1 / (double)data.Length) * 100.00, "Clustering - " + day[0].SchArrivalTime.ToShortDateString()));
                }
                progress.Report(new ProgressReporting(99.99, "Grouping Clusters"));
               //Once the clusters have been finalized then calculate the grouping within the clusters.
               clusters.ForEach(cluster => cluster.CalculateGrouping());
            });

            progress.Report(new ProgressReporting(100.00, "Operation Completed"));
            //Returns back an array of the results.
            return clusters.ToArray();
        }



        /// <summary>
        /// Given two sets of timetable records are they the same, if you ignore the day and only focus upon time.
        /// </summary>
        /// <param name="d1">Set of records one.</param>
        /// <param name="d2">Set of records two.</param>
        /// <returns></returns>
        private static bool IsSameSetofRecords(IBusTimeTable[] d1, IBusTimeTable[] d2)
        {
            //If they are not the same length then they are obviously not the same.
            if (d1.Length != d2.Length)
                return false;

            //Else go through the whole set of records and check each record if they both match.
            for (int i = 0; i < d1.Length; i++)
                if (!IsSameRecord(d1[i], d2[i]))
                    return false;
            
            //They were both the same timetables.
            return true;
        }

        /// <summary>
        /// Checks that two individual records share the same arrival and departure times. 
        /// </summary>
        /// <param name="t1">Record one.</param>
        /// <param name="t2">Record two.</param>
        /// <returns>True if two records are about the same time</returns>
        /// <remarks>
        ///     For some reason some services report a different route each day, this obviously shouldn't be possible so we check are the two stops the same or not.
        ///     This works most the time, unless the route really has changed, in which case there's a problem.
        /// </remarks>
        private static bool IsSameRecord(IBusTimeTable t1, IBusTimeTable t2)
        {
            return !t1.WeakIsStopSame(t2) || IsSameTime(t1.SchArrivalTime, t2.SchArrivalTime) && IsSameTime(t1.SchDepartureTime, t2.SchDepartureTime);
        }

        /// <summary>
        /// Checks that two records have the same time of day.
        /// </summary>
        /// <param name="d1">Record one.</param>
        /// <param name="d2">Record two.</param>
        /// <returns></returns>
        private static bool IsSameTime(DateTime d1, DateTime d2)
        {
            return d1.TimeOfDay.Equals(d2.TimeOfDay);
        }

    }
}
