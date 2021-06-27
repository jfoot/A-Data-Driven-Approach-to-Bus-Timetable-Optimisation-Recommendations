// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{

    /// <summary>
    /// The service cohesion evaluator works with the Evaluator class to calculate how well a services timetable works,
    /// with another service that shares a common route segment. By assigning a blame value to each timetable record
    /// based on how un-cohesive it is. 
    /// </summary>
    public class ServiceCohesionEvaluator
    {
        /// <value>A reference to the parent evaluator object.</value>
        private readonly TimeTableEvaluator _evaluator;

        /// <value>The fixed dominance value. </value>
        public static double Dominance { get; private set; } = Properties.Settings.Default.CohesionDominance;


        /// <summary>
        /// The default constructor for the service cohesion evaluator object,
        /// Which takes in the parent evaluator and the fixed dominance value.
        /// </summary>
        /// <param name="evaluator">Takes in the main evaluator object as reference</param>
        /// <param name="dominance">The dominance value for the cohesion blame value. Only use if you wish to override the settings value.</param>
        public ServiceCohesionEvaluator(TimeTableEvaluator evaluator, double? dominance = null)
        {
            _evaluator = evaluator;
            Dominance = dominance ?? Properties.Settings.Default.CohesionDominance;
        }
        
        /// <summary>
        /// Works out the service cohesion for services that share a common route-segments for specific bus stops.
        /// </summary>
        /// <returns>
        /// A dictionary, where bus service is key, and the value is an array of tuples of timetable records
        /// and blame values. Where.........
        /// </returns>
        public void FindBlameServiceCohesion(Solution solution)
        {
            //Reset all weights back to null before continuing, as a change anywhere means that everything needs to be changed.
            foreach (BlamedBusTimeTable record in solution.BusTimeTables.Values.SelectMany(x => x).ToList())
            {
                record.CohesionWeights.RawWeight = null;
                record.CohesionWeights.Weight = null;
            }


            //For each stop where there is a shared route segment.  
            foreach ((IBusStop busStop, List<IBusService> busServices) in _evaluator.Collection.ServicesAtStopOfInterest)
            {
                //Get all the timetable records at the stop of interest. 
                BlamedBusTimeTable[] recordsAtStop = GetRecordsAtStop(busStop, busServices, solution);
                
                //Group the records by the hour in which they are due to arrive.
                IEnumerable<IGrouping<int, BlamedBusTimeTable>> groupedData = recordsAtStop.GroupBy(r => r.SchArrivalTime.Hour);

                //Go through each hourly grouping of data.
                foreach (IGrouping<int, BlamedBusTimeTable> group in groupedData)
                {
                    //The number of buses due to turn up in the hour.
                    int count = group.Count();

                    //There is no need to do cohesion between only one service. 
                    if(count <= 1)
                        continue;

                    //The time spacing between all services if equally split within an hour.
                    TimeSpan minDiff = new TimeSpan(0, 60 / count, 0);

                    //The Time of day the service should aim for. It is only "lose" because this time cane be offset.
                    //It is more important that the spacing between services is consistent.
                    TimeSpan loseTargetTime = new TimeSpan(group.Key, 0, 0);

                    //This keeps track of all the difference between the lose target and the scheduled time.
                    List<double> timeDiffs = new();
                    foreach (BlamedBusTimeTable record in group)
                    {
                        //The difference between the target time and the current scheduled time.
                        TimeSpan deltaTime = loseTargetTime - record.SchArrivalTime.TimeOfDay;

                        //The difference between the target time and the scheduled time.
                        timeDiffs.Add(deltaTime.TotalMinutes);

                        //Temp set a raw weight, this will be changed later.
                        record.CohesionWeights.RawWeight = deltaTime.TotalMinutes;

                        //Updates the target time for the next service, such that 
                        loseTargetTime = loseTargetTime.Add(minDiff);
                    }
                    
                    //The average amount a record is off of the weak target time.
                    //This is so if they are all offset from an equal amount we do not care and can deduct this.
                    double avgDiff = timeDiffs.Average();
                    
                    //Goes through and works out the difference from the average value and use this as the weighting.
                    foreach (BlamedBusTimeTable record in group)
                    {
                        double diffOffAvg = (record.CohesionWeights.RawWeight ?? avgDiff) - avgDiff;
                        
                        //Sets all of the weights accordingly. 
                        record.CohesionWeights.Weight = Math.Abs(diffOffAvg);
                        record.CohesionWeights.RawWeight = diffOffAvg;
                        record.CohesionWeights.TargetSchArrivalTime = record.SchArrivalTime.AddMinutes(diffOffAvg);
                        record.CohesionWeights.TargetSchDepartureTime = record.SchDepartureTime.AddMinutes(diffOffAvg);
                    }
                }
            }

            //Standarise values and apply the dominance. 
            StandardiseValues(solution);
        }


        /// <summary>
        /// Takes in an array of unstandardised blame records, and then standardises their cohesion value,
        /// such that it can be compared to other blame values. It will also adjust this value, such that the
        /// dominance acts on the value.
        /// </summary>
        /// <returns>Adjusts the blamed-bus timetable records such that they have standardised cohesion values.</returns>
        private static void StandardiseValues(Solution solution)
        {
            //Flattens the 2D array into one, gets all timetable records of all services.
            BlamedBusTimeTable[] unstandardised = solution.BusTimeTables.Values.SelectMany(x => x).ToArray();

            (double? minValue, double? maxValue) = FilterValues(unstandardised);
         
            //If any of these values are null, then there is no cohesion between any of the services, so return.
            if (minValue == null || maxValue == null)
                return;

            //Find range between max and min for the MinMax Scaling algorithm.
            double range = (double)(maxValue - minValue);

            //If there is only one weight, then there is nothing to standardise. 
            if (range == 0)
                return;

           
            //For each timetable blame record, standardise it's value and also apply the dominance value.
            foreach (BlamedBusTimeTable record in unstandardised)
                record.CohesionWeights.Weight = (((record.CohesionWeights.Weight ?? 0) - minValue) / range) * Dominance;
        }






        /// <summary>
        /// This will replace the bottom 5% and the top 95% of the cohesion weights with the average value. 
        /// This is done so that any outliers which are probably erroneous are removed.
        /// </summary>
        /// <param name="unfilteredTimeTables">The timetable which has the original unfiltered data.</param>
        /// <returns>Will alter the timetable array passed to it and also return the new min and max values set.</returns>
        private static (double? minValue, double? maxValue) FilterValues(BlamedBusTimeTable[] unfilteredTimeTables)
        {
            //The size of the new filtered array.
            int percentileCount = (int)(unfilteredTimeTables.Length * 0.9);
            //Creates a new array to store the shortened filtered results into.
            BlamedBusTimeTable[] tempFiltered = new BlamedBusTimeTable[percentileCount];

            //Copies only the range between 10% to 90%, dropping the first and last most erroneous values. 
            Array.Copy(unfilteredTimeTables.OrderBy(r => r.CohesionWeights.Weight).ToArray(), (int)(unfilteredTimeTables.Length * 0.05), tempFiltered, 0, percentileCount);

            //Average Value of the filtered/ reduced data set.
            double? avgValue = tempFiltered.Average(r => r.CohesionWeights.Weight);

            //The minimum slack weight. (5%) then set all values which were below this to 5%.
            double? minValue = tempFiltered.Where(r => r.CohesionWeights.Weight != null).Min(r => r.CohesionWeights.Weight);
            foreach (BlamedBusTimeTable blamedBusTimeTable in unfilteredTimeTables.Where(r => r.CohesionWeights.Weight < minValue))
                blamedBusTimeTable.CohesionWeights.Weight = avgValue;

            //The maximum slack weight, (95%) then set all values that were above this to 95%.
            double? maxValue = tempFiltered.Max(r => r.CohesionWeights.Weight);
            foreach (BlamedBusTimeTable blamedBusTimeTable in unfilteredTimeTables.Where(r => r.CohesionWeights.Weight > maxValue))
                blamedBusTimeTable.CohesionWeights.Weight = avgValue;

            //For any stops that have a null cohesion value set it to be the average value.
            //This is so the overall weighted average isn't skewed. 
            unfilteredTimeTables.Where(r => !r.CohesionWeights.Weight.HasValue).ToList()
                .ForEach(r => r.CohesionWeights.Weight = avgValue);

            return (minValue, maxValue);
        }





        /// <summary>
        /// Given a specific stop and a list of services of interest at the stop, get all the timetable records,
        /// from all of those services and return any which are specifically about that stop.
        /// </summary>
        /// <param name="stop">The stop of interest to get timetable records for</param>
        /// <param name="services">A list of services that we know stop at the bus stop.</param>
        /// <returns>An array of all the timetable records at the stop.</returns>
        private static BlamedBusTimeTable[] GetRecordsAtStop(IBusStop stop, List<IBusService> services, Solution solution)
        {
            List<BlamedBusTimeTable> recordsAtStop = new();
            foreach (IBusService? service in services)
                if(solution.BusTimeTables.ContainsKey(service))
                    recordsAtStop.AddRange(solution.BusTimeTables[service].Where(record => record.WeakIsStopSame(stop)).ToList());

            //All the timetable records at the stop, in order of arrival time and only for services of interest to us.
            return recordsAtStop.OrderBy(record => record.SchArrivalTime).ToArray();
        }
    }
}
