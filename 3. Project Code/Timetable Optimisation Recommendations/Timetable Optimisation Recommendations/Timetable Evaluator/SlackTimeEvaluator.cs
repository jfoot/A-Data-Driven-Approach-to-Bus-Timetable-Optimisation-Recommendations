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
using Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search;
using Timetable_Optimisation_Recommendations.Timetable_Simulator;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{
    /// <summary>
    /// The slack time evaluator works with the Evaluator class to calculate if the timetable has excessive slack
    /// (Or not enough). By assigning a blame value to each timetable record based on how disruptive it is to the
    /// timetable. 
    /// </summary>
    public class SlackTimeEvaluator
    {
        /// <value>A reference to the parent evaluator object.</value>
        private readonly TimeTableEvaluator _evaluator;

        /// <value>The fixed dominance value. </value>
        public static double Dominance { get; private set; } = Properties.Settings.Default.SlackTimeDominance;

        /// <summary>
        /// Default constructor for the slack time evaluator.
        /// </summary>
        /// <param name="evaluator">The evaluator object, which stores the current proposed solution.</param>
        /// <param name="dominance">The dominance value for the slack time blame value. Only use if you wish to override the settings value.</param>
        public SlackTimeEvaluator(TimeTableEvaluator evaluator, double? dominance = null)
        {
            _evaluator = evaluator;
            Dominance = dominance ?? Properties.Settings.Default.SlackTimeDominance;
        }


        /// <summary>
        /// Will calculate the slack time blame values for all of the services needed in the evaluator 
        /// </summary>
        /// <param name="solution">The solution you wish to apply slack time evaluator too. By defualt this will be the one in the evualtor.</param>
        /// <param name="progress">Used to report back the total progress of the task.</param>
        /// <returns>Once completed all blame records will have a slack value blame.</returns>
        public async Task FindBlameSlackTime(Solution solution, IProgress<AdvancedProgressReporting>? progress = null)
        {

            //All of this code is used for the progress bar.
            int totalCompletedTasks = 0;
            int totalTasks = solution.BusTimeTables.Keys.Count;

            Progress<ProgressReporting> subProgress = new();
            subProgress.ProgressChanged += delegate (object? o, ProgressReporting d)
            {
                progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0 + (1.0 / totalTasks * d.Value),
                    d.Value, d.Message));
            };

         
            //This then goes through every service in the records and evaluates their slack time blame.
            foreach ((IBusService _, BlamedBusTimeTable[] records) in solution.BusTimeTables)
            {
                await FindSingleBlameSlackTime(records, subProgress);
                ++totalCompletedTasks;
            }

            StandardiseSolution(solution);
        }



        /// <summary>
        /// Given a day worth of timetable data go through every record and then assign a blame value to each record.
        /// Return this list of tuples to then decide what problem areas to focus upon.
        /// </summary>
        /// <param name="serviceTimetable">A days worth of timetable data.</param>
        /// <param name="progress">A progress reporter to update GUI.</param>
        /// <remarks>
        ///     If you call this function you MUST standardised the results after wards.
        ///     This isn't done within the function itself encase you call it several times over.
        ///     Then you only need to call it after you last call to the function.
        /// </remarks>
        /// <returns>An array of timetable records and blame values.</returns>
        public async Task FindSingleBlameSlackTime(BlamedBusTimeTable[] serviceTimetable, IProgress<ProgressReporting>? progress = null)
        {
            if (serviceTimetable.Length == 0)
            {
                Console.WriteLine("No Timetable data found for the service.");
                return;
            }

            //Group all the values by their running boards, these are sequential trips performed by the same driver.
            IOrderedEnumerable<IGrouping<string, BlamedBusTimeTable>> query = from record in serviceTimetable
                                                                         group record by record.RunningBoard into groups
                                                                         orderby groups.Key
                                                                         select groups;

            //Keeps track of overall progress.
            //This could be improved to record the sub-progress.
            int totalCompleted = 0;

            //Go through each running board worth of data within the timetable records.
            await query.ParallelForEachAsync(async (data) =>
            {
                //The scheduled timetable.
                BlamedBusTimeTable[] schBusTimeTables = data.ToArray();
                //The theoretical minimum timetable. 
                BusTimeTableStub[] thoMinRecords = await GenerateEstimatedTimesAsync(schBusTimeTables);
                
                //Calculate the blame and add the records back into the list.
                CalculateBlame(schBusTimeTables, thoMinRecords);

                Interlocked.Add(ref totalCompleted, schBusTimeTables.Length);
                progress?.Report(new ProgressReporting(totalCompleted / (double)serviceTimetable.Length * 100, "Completed Blame for Service " + schBusTimeTables.First().Service.ServiceId +  ", Running Board " + data.Key));
            }, maxDegreeOfParallelism: 3);


            progress?.Report(new ProgressReporting(100, "Slack Time Evaluation for Service " + serviceTimetable.First().Service.ServiceId + " completed and standardiesed."));
        }


        /// <summary>
        /// Takes in an array of unstandardised blame records, and then standardises their slack time value,
        /// such that it can be compared to other blame values. It will also adjust this value, such that the
        /// pareto-dominance acts on the value.
        /// </summary>
        /// <param name="unstandardised">An array of unstandardised blame values.</param>
        /// <remarks>
        /// Changing any timetable record, would effect all other blame records, regardless of if it was in the
        /// journey group or not. As such the unstandardised values needs to be cached.
        /// </remarks>
        public static void StandardiseSolution(Solution unstandardised)
        {
            //Flattens the 2D array into one, gets all timetable records of all services.
            BlamedBusTimeTable[] unstandardisedFlattened = unstandardised.BusTimeTables.Values.SelectMany(x => x).ToArray();

            //Gets the min and max slack value in the solution.
            (double min, double max) = FilterValues(ref unstandardisedFlattened);

            //Gets the range of values in the solution.
            double range = max - min;

            //If there is only one weight, then there is nothing to standardise. 
            if (range == 0)
                return;

            //MinMax Scaling and Pareto dominance adjustments
            foreach (BlamedBusTimeTable record in unstandardisedFlattened)
                record.SlackWeights.Weight = ((record.SlackWeights.Weight - min) / range) * Dominance;
        }


        /// <summary>
        /// This will replace the bottom 5% and the top 95% of the slack times with the average value.
        /// This is done so that any outliers which are probably erroneous are removed.
        ///
        ///
        /// IMPORTANT!
        /// This will also change ALL normalized weights back to their absolute raw weights, so they are
        /// no longer normalized and needs changing!
        /// </summary>
        /// <param name="unfilteredTimeTables">The timetable which has the original unfiltered data.</param>
        /// <returns>Will alter the timetable array passed to it and also return the new min and max values set.</returns>
        private static (double minValue, double maxValue) FilterValues(ref BlamedBusTimeTable[] unfilteredTimeTables)
        {
            //The size of the new filtered array.
            int percentileCount = (int)(unfilteredTimeTables.Length * 0.9);
            //Creates a new array to store the shortened filtered results into.
            BlamedBusTimeTable[] tempFiltered = new BlamedBusTimeTable[percentileCount];

            //Copies only the range between 5% to 95%, dropping the first and last most erroneous values. 
            Array.Copy(unfilteredTimeTables.OrderBy(r => Math.Abs(r.SlackWeights.RawWeight ?? 0)).ToArray(), (int)(unfilteredTimeTables.Length * 0.05), tempFiltered, 0, percentileCount);
            

            //Sets the normalized weights back to their raw weights. 
            //Not currently normalized so need to be changed later.
            foreach (BlamedBusTimeTable record in unfilteredTimeTables)
                record.SlackWeights.Weight = Math.Abs(record.SlackWeights.RawWeight ?? 0);

            // Then corrects the outliers results, the bottom and top 5%, giving them the average values instead. 
            //Average Value of the filtered/ reduced data set.
            double avgValue = tempFiltered.Average(r => Math.Abs(r.SlackWeights.RawWeight ?? 0));

            //The minimum slack weight. (5%) then set all values which were below this to 5%.
            double minValue = tempFiltered.Min(r => Math.Abs(r.SlackWeights.RawWeight ?? 0));
            foreach (BlamedBusTimeTable blamedBusTimeTable in unfilteredTimeTables.Where(r => Math.Abs(r.SlackWeights.RawWeight ?? 0) < minValue))
                blamedBusTimeTable.SlackWeights.Weight = avgValue;

            //The maximum slack weight, (95%) then set all values that were above this to 95%.
            double maxValue = tempFiltered.Max(r =>  Math.Abs(r.SlackWeights.RawWeight ?? 0));
            foreach (BlamedBusTimeTable blamedBusTimeTable in unfilteredTimeTables.Where(r => Math.Abs(r.SlackWeights.RawWeight ?? 0) > maxValue))
                blamedBusTimeTable.SlackWeights.Weight = avgValue;
            
            //Return back the min and max value found.
            return (minValue, maxValue);
        }


        /// <summary>
        /// This will return an array of tuples of timetable records, along with a double blame value.
        /// The higher the blame value the higher it is an issue in the timetable. The blame value is the
        /// difference between the previous value's difference between the theoretical time and actual time.
        /// </summary>
        /// <param name="schBusTimeTables">An array of the scheduled timetable values.</param>
        /// <param name="thoMinRecords">An array of theoretical minimum values.</param>
        /// <returns>An array of tuples of timetable records and blame values.</returns>
        private static void CalculateBlame(BlamedBusTimeTable[] schBusTimeTables, BusTimeTableStub[] thoMinRecords)
        {
            //For every bus stop/record in the timetable.
            for (int i = 0; i < schBusTimeTables.Length; i++)
            {
                //The time difference between the scheduled and the theoretical arrival times. 
                double differenceInTimes = (thoMinRecords[i].SchArrivalTime - schBusTimeTables[i].SchArrivalTime).TotalMinutes + (thoMinRecords[i].SchDepartureTime - schBusTimeTables[i].SchDepartureTime).TotalMinutes;
                //Calculate the change rate of the difference between the two values.
                schBusTimeTables[i].SlackWeights.Weight = Math.Abs(differenceInTimes);
                schBusTimeTables[i].SlackWeights.RawWeight = differenceInTimes;
                schBusTimeTables[i].SlackWeights.TargetSchArrivalTime = thoMinRecords[i].SchArrivalTime;
                schBusTimeTables[i].SlackWeights.TargetSchDepartureTime = thoMinRecords[i].SchDepartureTime;
            }
        }


        /// <summary>
        /// Generates the average time a vehicle has needed to actually travel and dwell at every stop throughout a route.
        /// This can be thought upon as the minimum amount of slack time needed to wait, while still having an "on time" service.
        /// </summary>
        /// <param name="plannedTimetable">A selection of time table records all from the same service. They should be sequential and in order of stops visited.</param>
        /// <param name="progress">Used to report back the progress of the task as this can be a slow process.</param>
        /// <returns>An array of stub timetable records, representing the theoretical minimum value.</returns>
        private async Task<BusTimeTableStub[]> GenerateEstimatedTimesAsync(BlamedBusTimeTable[] plannedTimetable, IProgress<double>? progress = null)
        {
            //The very first record is always going to be a fixed event. I.e, the bus will always start at this time.
            List<BusTimeTableStub> minimumAverageTime = new() { new BusTimeTableStub(plannedTimetable[0]) };


            for (int i = 1; i < plannedTimetable.Length; i++)
            {
                //The last departure time of the service.
                TimeSpan lastDepartureTime = plannedTimetable[i - 1].SchDepartureTime.TimeOfDay;
                //The last stop it visited.
                IBusStop lastStop = plannedTimetable[i - 1].Location;
                //The stop it is currently traveling to.
                IBusStop nextStop = plannedTimetable[i].Location;

                //Gets all the services that go between the two stops and have been chosen as of interest.
                IBusService[] servicesOfRelevance = _evaluator.Collection.GetServices(lastStop, nextStop);

                //You could theoretically get ALL services that go between these two stops, even if a user hasn't choose to select them for optimizing
                //I have made the decision to not bother with this, one because I would need to keep track of the difference between what a user has chosen and what exists and two if the user hasn't chosen it.
                //They most likely don't wont to wait the extra time. Doing all services would take ages. 
                var journey = new JourneyTimeSimulator(lastDepartureTime, lastStop, nextStop, servicesOfRelevance,
                    _evaluator.RelatedDates);

                //The estimated time to travel between the two stops.
                TimeSpan timeToTravel = await journey.ProduceEstimatedTravelTimes(progress);
                //The arrival time, which is equal to the last departure time + the time to travel.
                TimeSpan arrivalTime = lastDepartureTime + timeToTravel;

                //Creates a new dwell simulator, this does simplify it slightly as we assume here that it is not the end or start of a route segment. Otherwise, we couldn't really include them all.
                var dwell = new DwellTimeSimulator(_evaluator.RelatedDates, nextStop, arrivalTime,
                    nextStop.GetServices());
                //The estimated dwell time at the stop, to pick up or drop off passengers.
                TimeSpan dwellTimeRequired = await dwell.ProduceEstimatedDwell(progress);

                //The departure time estimated, which is equal to the arrival time + dwell time.
                TimeSpan departureTime = arrivalTime + dwellTimeRequired;

                //Adds the new theoretical record to the timetable.
                minimumAverageTime.Add(new BusTimeTableStub(plannedTimetable[i], arrivalTime, departureTime));
            }

            //Returns results
            return minimumAverageTime.ToArray();
        }
    }
}
