// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Route_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Performance_Evaluator;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{
    /// <summary>
    /// Pre-evaluator checks is run before actually running the real evaluator and is used
    /// to download all of the data that is required and evaluate the performance of the old timetable. 
    /// </summary>
    public class PreEvaluatorChecks
    {
        ///<value>All the dates for which data needs to be collected.</value>
        private DateTime[] RelatedDates { get; }

        ///<value>The starting timetables of all services, the initial solution to the problem.</value>
        private Dictionary<IBusService,IBusTimeTable[]> StartingTimetables { get; } = new();
        
        ///<value>Stores the collection of all the route segments.</value>
        private RouteSegmentCollection SegmentCollection { get; }

        ///<value>Performance evaluator generates key metrics of the services current timetables.</value>
        public PerformanceEvaluator PerformanceEvaluator { get; } = new();

        ///<value>Keeps track of how many tasks to complete.</value>
        private int _totalTasks = 0;
        ///<value>Keeps track of how many tasks have been completed.</value>
        private int _totalCompletedTasks = 0;
        ///<value>Keeps track of if it's been initialized or not before, no point doing it twice.</value>
        private bool _isIntilaised = false;

        /// <summary>
        /// The default constructor for the class, takes in the objects created from the previous steps.
        /// </summary>
        /// <param name="cluster">Stores information about the dates to request data for.</param>
        /// <param name="segmentCollection">Stores information about the route segments.</param>
        public PreEvaluatorChecks(Cluster cluster, RouteSegmentCollection segmentCollection)
        {
            RelatedDates = cluster.AssociatedTimes.ToArray();
            SegmentCollection = segmentCollection;
        }

        /// <summary>
        /// Produces the evaluator object from the pre-evaluator.
        /// </summary>
        /// <returns>The final evaluator to actually perform the search</returns>
        /// <remarks>
        /// You shouldn't call this before DownloadAllFilesNeeded has been called and completed.
        /// </remarks>
        public TimeTableEvaluator EvaluateTimeTable()
        {
            if (!_isIntilaised)
                throw new Exception(
                    "Initialization of the pre-evaluator was not called or had not completed, please do so first.");

            return new TimeTableEvaluator(RelatedDates, SegmentCollection, StartingTimetables);
        }


        /// <summary>
        /// Does the work, downloads all of the data needed so that it is in tire 2 and 1 cache.
        /// Also works out the performance metrics of this data.
        /// </summary>
        /// <param name="progress">Used to report back the progress to the GUI.</param>
        /// <returns>Caches all of the required data for the search.</returns>
        public async Task DownloadAllFilesNeeded(IProgress<AdvancedProgressReporting>? progress)
        {
            if (!_isIntilaised)
            {
                Progress<ProgressReporting> subProgress = new();
                subProgress.ProgressChanged += delegate(object? o, ProgressReporting d)
                {
                    progress?.Report(new AdvancedProgressReporting(_totalCompletedTasks / (double)_totalTasks * 100.0 + (1.0 / _totalTasks * d.Value), d.Value, d.Message));
                };

                IBusService[] services = SegmentCollection.IncludedServices.ToArray();

                _totalCompletedTasks = 0;
                _totalTasks = services.Length + 1;

                //Downloads the service cache data.
                foreach (IBusService? service in services)
                {
                    await DownloadService(subProgress, service);
                    progress?.Report(new AdvancedProgressReporting(Interlocked.Increment(ref _totalCompletedTasks) / (double)_totalTasks * 100.0, 100.0, "Successfully Downloaded " + service.ServiceId + " data required"));
                }

                //Downloads the stop cache data.
                if (Properties.Settings.Default.WeakStop)
                    await DownloadStopWeak(subProgress);
                else
                    await DownloadStop(subProgress);

                //Generate the performance metrics of the timetables.
                PerformanceEvaluator.GenerateLatenessReport();
                _isIntilaised = true;
            }

            progress?.Report(new AdvancedProgressReporting(100.0, 100.0, "Operation Completed"));
        }

        /// <summary>
        /// Downloads and caches the stop data as required. This is non-weak, so actually calls upon the API source.
        /// </summary>
        /// <param name="progress">used to report to the GUI the progress of the task.</param>
        /// <returns>Caches all stop data.</returns>
        private async Task DownloadStop(IProgress<ProgressReporting>? progress)
        {
            IBusStop[] stops = SegmentCollection.GetSharedBusStopsAsync();

            int totalCompletedSubTasks = 0;
            int totalSubTasks = RelatedDates.Length * stops.Length;

            foreach (IBusStop? stop in stops)
            {
                await RelatedDates.ParallelForEachAsync(async (date) =>
                {
                    _ = await stop.GetArchivedTimeTable(date);
                    progress?.Report(new ProgressReporting((Interlocked.Increment(ref totalCompletedSubTasks) / (double)totalSubTasks) * 100.0, "Successfully Downloaded " + stop.CommonName + " data on the " + date.ToShortDateString()));
                }, maxDegreeOfParallelism: 3);
            }
        }

        /// <summary>
        /// Downloads and caches the stop data as required. This is weak, so doesn't
        /// actually call API and builds data source from service data.
        /// </summary>
        /// <param name="progress">used to report to the GUI the progress of the task.</param>
        /// <returns>Caches all stop data.</returns>
        private async Task DownloadStopWeak(IProgress<ProgressReporting> progress)
        {
            IBusStop[] stops = SegmentCollection.GetSharedBusStopsAsync();

            int totalCompletedSubTasks = 0;
            int totalSubTasks = RelatedDates.Length * stops.Length;

            foreach (IBusStop? stop in stops)
            {
                await RelatedDates.ParallelForEachAsync(async (date) =>
                {
                    _ = await stop.GetWeakArchivedTimeTable(date);
                    progress.Report(new ProgressReporting((Interlocked.Increment(ref totalCompletedSubTasks) / (double)totalSubTasks) * 100.0, "Successfully Generated Weak Stop Data at " + stop.CommonName + " data on the " + date.ToShortDateString()));
                }, maxDegreeOfParallelism: 5);
            }
        }


        /// <summary>
        /// Downloads all of the services historical timetable data for the dates required.
        /// </summary>
        /// <param name="progress">Used to report to the GUI the progress of the task.</param>
        /// <param name="service">The service in quest to request data for.</param>
        /// <returns>Downloads and Caches services timetable data.</returns>
        private async Task DownloadService(IProgress<ProgressReporting> progress, IBusService service)
        {
            int totalCompletedSubTasks = 0;
            int totalSubTasks = RelatedDates.Length + 1;

            StartingTimetables.Add(service,await FindStartingTimetable(service));
            progress.Report(new ProgressReporting((Interlocked.Increment(ref totalCompletedSubTasks) / (double)totalSubTasks) * 100.0, "Service " + service.ServiceId + " successfully got Planned Timetable " + RelatedDates.First().ToShortDateString()));

            await RelatedDates.ParallelForEachAsync(async (date) =>
            {
                PerformanceEvaluator.AddRecords(service, await service.GetArchivedTimeTable(date));
                progress.Report(new ProgressReporting((Interlocked.Increment(ref totalCompletedSubTasks) / (double)totalSubTasks) * 100.0,"Service " + service.ServiceId + " successfully got Timetable " + date.ToShortDateString()));
            }, maxDegreeOfParallelism: 3);
        }


        /// <summary>
        /// Downloads the services Scheduled timetable data for the dates required.
        /// </summary>
        /// <param name="service">The service in quest to request data for.</param>
        /// <returns>The services inital starting timetable.</returns>
        private async Task<IBusTimeTable[]> FindStartingTimetable(IBusService service)
        {
            //For each date continue until a timetable can be found.
            foreach (DateTime date in RelatedDates)
            {
                IBusTimeTable[]? temp = await service.GetTimeTable(date);
                if (temp != null)
                    return temp;
            }

            Console.WriteLine("Warning : No Starting Timetable could be found");
            return Array.Empty<IBusTimeTable>();
        }
    }
}
