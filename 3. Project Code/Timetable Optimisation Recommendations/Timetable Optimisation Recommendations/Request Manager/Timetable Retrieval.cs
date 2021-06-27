using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Request_Manager
{
    /// <summary>
    /// Used to get a batch/ multiple-days worth of timetable data in a single query. 
    /// However, this still has to be done sequentially because the Reading Buses API doesn't like having more than one database connection open at once.
    /// </summary>
    public class TimetableRetrieval
    {
        //The maximum number of concurrent threads making calls to the API source.
        private static readonly int MaxParallelism = 3;


        /// <summary>
        /// Gets multiple days worth of historical time table data for a particular service between two date and times.
        /// </summary>
        /// <param name="progress">Used to return back to the GUI the current progress of the task.</param>
        /// <param name="start">The start date, should be oldest into the past.</param>
        /// <param name="end">The end date for when you want to go up to, inclusive.</param>
        /// <param name="service">The service for which you want historical time table data for.</param>
        /// <returns>Gets all the days of historic timetable data between the two dates inclusive.</returns>
        /// <remarks>
        /// The days will not necessarily be in order, they are in the order in which the API sent data.
        /// If the ordering is important you must re-order it.
        /// </remarks>
        public static async Task<IBusHistoricTimeTable[][]> GetHistoricTimeTableBatch(DateTime start, DateTime end, IBusService service, IProgress<double>? progress = null)
        {
            //Stores the results
            List<IBusHistoricTimeTable[]?> timeTableData = new();

            //Create an array of all the dates between the range inclusive.
            DateTime[] dates = Enumerable.Range(0, 1 + end.Subtract(start).Days).Select(offset => start.AddDays(offset)).ToArray();

            await dates.ParallelForEachAsync(async (date) =>
            {
                timeTableData.Add(await service.GetArchivedTimeTable(date));
                progress?.Report(timeTableData.Count / ((end - start).TotalDays + 1) * 100);
            }, maxDegreeOfParallelism: MaxParallelism);

            //Removes any null data.
            timeTableData.RemoveAll(x => x == null || x.Length == 0);
            //Reports finished and returns data to an array before returning.
            progress?.Report(100);
            return timeTableData.ToArray()!;
        }


        /// <summary>
        /// Gets multiple days worth of planned time table data for a particular service between two dates and times.
        /// </summary>
        /// <param name="progress">Used to return back to the GUI the current progress of the task.</param>
        /// <param name="start">The start date, should be oldest into the past.</param>
        /// <param name="end">The end date for when you want to go up to, inclusive.</param>
        /// <param name="service">The service for which you want planned time table data for.</param>
        /// <returns>Gets all the days of timetable data between the two dates inclusive.</returns>
        /// <remarks>
        /// The days will not necessarily be in order, they are in the order in which the API sent data.
        /// If the ordering is important you must re-order it.
        /// </remarks>
        public static async Task<IBusTimeTable[][]> GetTimeTableBatch(DateTime start, DateTime end, IBusService service, IProgress<ProgressReporting>? progress = null)
        {
            //Stores the results
            List<IBusTimeTable[]?> timeTableData = new();
            //Create an array of all the dates between the range inclusive.
            DateTime[] dates = Enumerable.Range(0, 1 + end.Subtract(start).Days).Select(offset => start.AddDays(offset)).ToArray();

            await dates.ParallelForEachAsync(async (date) =>
            {
                timeTableData.Add(await service.GetTimeTable(date));
                progress?.Report(new ProgressReporting(timeTableData.Count / ((end - start).TotalDays + 1) * 100, "Successfully got timetable - " + date.ToShortDateString()));
            }, maxDegreeOfParallelism : MaxParallelism);

            //Removes any null data.
            timeTableData.RemoveAll(x => x == null || x.Length == 0);
            //Reports finished and returns data to an array before returning.
            progress?.Report(new ProgressReporting(100, "Completed Operation"));

            return timeTableData.ToArray()!;
        }





        /// <summary>
        /// Gets multiple days worth of historical time table data for a particular service between two date and times.
        /// </summary>
        /// <param name="progress">Used to return back to the GUI the current progress of the task.</param>
        /// <param name="stop">The stop for which you want historical time table data for.</param>
        /// <param name="cluster">Used to get back data for a service at a specif cluster of dates.</param>
        /// <returns></returns>
        public static async Task<IBusSolidHistoricTimeTable[][]> GetHistoricTimeTableBatch(DateTime[] cluster, IBusStop stop, IProgress<double>? progress = null)
        {
            //Stores the results
            List<IBusSolidHistoricTimeTable[]> timeTableData = new();


            //Goes through every day in the cluster and gets the archived timetable for that day
            //Updates progress as needed and limits parallelism. 
            await cluster.ParallelForEachAsync(async (date) =>
            {
                //Get the archived data.
                IBusHistoricTimeTable[]? temp = Properties.Settings.Default.WeakStop ? await stop.GetWeakArchivedTimeTable(date) : await stop.GetArchivedTimeTable(date);
                //Convert to solid format, removing any values that don't have actual arrival and departure information.
                if (temp != null)
                    timeTableData.Add(ConvertToSolidArray(temp));
                progress?.Report(timeTableData.Count / (double)cluster.Length * 100);
            }, maxDegreeOfParallelism: MaxParallelism);

            
            //Reports finished and returns data to an array before returning.
            progress?.Report(100);
            return timeTableData.ToArray();
        }



        public static async Task<IBusSolidHistoricTimeTable[][]> GetHistoricTimeTableBatch(DateTime[] cluster, IBusService service, IProgress<double>? progress = null)
        {
            //Stores the results
            List<IBusSolidHistoricTimeTable[]> timeTableData = new();


            //Goes through every day in the cluster and gets the archived timetable for that day
            //Updates progress as needed and limits parallelism. 
            await cluster.ParallelForEachAsync(async (date) =>
            {
                //Get the archived data.
                IBusHistoricTimeTable[]? temp = await service.GetArchivedTimeTable(date);
                //Convert to solid format, removing any values that don't have actual arrival and departure information.
                if (temp != null)
                    timeTableData.Add(ConvertToSolidArray(temp));
                progress?.Report((timeTableData.Count / (double)cluster.Length) * 100);
            }, maxDegreeOfParallelism: MaxParallelism);

            //Reports finished and returns data to an array before returning.
            progress?.Report(100);
            return timeTableData.ToArray();
        }



        /// <summary>
        /// Converts a list of non-solid historical time table records into solid timetable records.
        /// </summary>
        /// <param name="nonSolid">An array of non-solid timetable records, which could contain records with missing actual arrival and departure times.</param>
        /// <returns>Returns an array of solid timetable records, with any non-solid records removed.</returns>
        private static IBusSolidHistoricTimeTable[] ConvertToSolidArray(IBusHistoricTimeTable[] nonSolid)
        {
            List<IBusSolidHistoricTimeTable> solidTimes = new();
            foreach (IBusHistoricTimeTable? record in nonSolid.Where(record => record != null && record.CouldBeSolid()).ToArray())
                solidTimes.Add(record.GetSolid());

            return solidTimes.ToArray();
        }
    }
}
