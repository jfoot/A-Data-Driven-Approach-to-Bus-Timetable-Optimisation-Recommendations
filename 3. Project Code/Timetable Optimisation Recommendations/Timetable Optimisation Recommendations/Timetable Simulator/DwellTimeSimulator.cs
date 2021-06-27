// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Request_Manager;

namespace Timetable_Optimisation_Recommendations.Timetable_Simulator
{
    /// <summary>
    /// Used to calculate how long a bus is going to need to dwell at a bus stop, given the time of day
    /// and hence changes in passenger demand. Can be considered how busy a stop is.
    /// </summary>
    public class DwellTimeSimulator : TimeSimulator
    {
        ///<value>Dates for which we can request data from.</value>
        private readonly DateTime[] _dataCluster;
        ///<value>The bus stop in question to get dwell time for.</value>
        private readonly IBusStop _busStop;
        ///<value>The time a bus is meant to arrive at the stop.</value>
        private readonly TimeSpan _arrivalTime;
        ///<value>What bus services stopping at the stop were interested in.</value>
        private readonly IBusService[] _busService;

        /// <summary>
        /// The default constructor for the class.
        /// </summary>
        /// <param name="cluster">Dates for which we can request data from</param>
        /// <param name="busStop">The bus stop in question to get dwell time for</param>
        /// <param name="time">The time a bus is meant to arrive at the stop</param>
        /// <param name="services">What bus services stopping at the stop were interested in</param>
        public DwellTimeSimulator(DateTime[] cluster, IBusStop busStop, TimeSpan time, IBusService[] services)
        {
            _dataCluster = cluster;
            _busStop = busStop;
            _busService = services;
            _arrivalTime = time;
        }


       /// <summary>
       /// Actually generates the time that is estimated for the bus to dwell at the stop.
       /// </summary>
       /// <param name="progress">Progress bar used to update the GUI on the progress of the task.</param>
       /// <returns>Given the starting parameters the final outputted dwell time estimate.</returns>
        public async Task<TimeSpan> ProduceEstimatedDwell(IProgress<double>? progress)
        {
            //Requests the historic timetable for the bus stop and only the bus stop on every day in the cluster.
            IBusSolidHistoricTimeTable[][] data = await TimetableRetrieval.GetHistoricTimeTableBatch(_dataCluster, _busStop, progress);

            //Find all Dwell Times directly before and directly after our target time.
            List<(IBusSolidHistoricTimeTable, IBusSolidHistoricTimeTable)> pairings = FindPairingBetweenTime(data);


            //A list of all the weighted dwell times, one for each day.
            List<(TimeSpan, double)> dwellTimes = new();
            foreach((IBusSolidHistoricTimeTable, IBusSolidHistoricTimeTable) pair in pairings)
            {
                TimeSpan t1 = GetDwellTime(pair.Item1);
                TimeSpan t2 = GetDwellTime(pair.Item2);

                //Time difference between R1 and R2
                TimeSpan totalDifference = (pair.Item2.ActArrivalTime - pair.Item1.ActArrivalTime);
                //Time between R1 arriving and our goal.
                TimeSpan d1 = _arrivalTime - pair.Item1.ActArrivalTime.TimeOfDay;
                //Time between R2 arriving and our goal.
                TimeSpan d2 = pair.Item2.ActArrivalTime.TimeOfDay - _arrivalTime;


                try
                {
                    //Generate weighted average of result.
                    dwellTimes.Add(totalDifference.TotalSeconds == 0 ? 
                        ((t1 + t2) / 2, CalculateInverseWeight(d1.TotalSeconds)) 
                        : (t1 * (d1 / totalDifference) + t2 * (d2 / totalDifference), CalculateInverseWeight(d1.TotalSeconds, d2.TotalSeconds)));
                }
                catch (Exception)
                {
                    Console.WriteLine("Error " + totalDifference + " t1 : " + t1 +  "    d1 " + d1 + "     d2 " + d2 );
                }
            }

            //Generates the mean of all the values.
            return GenerateWeightedAverage(dwellTimes);
        }

        /// <summary>
        /// Given an array of days of data, find the two records on every day that are between the two time points of interest.
        /// </summary>
        /// <param name="data">An array of arrays, for the days of data.</param>
        /// <returns>A list of tuples, for the record directly before and after on every day.</returns>
        private List<(IBusSolidHistoricTimeTable, IBusSolidHistoricTimeTable)> FindPairingBetweenTime(IBusSolidHistoricTimeTable[][] data)
        {
            //A tuple of Historic Records, the first record directly before our time of interest and the record directly after our time of interest.
            List<(IBusSolidHistoricTimeTable, IBusSolidHistoricTimeTable)> pairings = new();

            //Go through each day worth of data.
            foreach (IBusSolidHistoricTimeTable[] day in data)
            {
                //Filtered to only contain the services we're interested in. 
                IBusSolidHistoricTimeTable[] dayFiltered = day.Where(record => _busService.Contains(record.Service)).ToArray();

      
                //Then go through every record of data in the day.
                for (int i = 1; i < dayFiltered.Length; i++)
                {
                    //Once you have found the first record that has a later actual arrival time than the time we are looking for, get that record and the one directly before.
                    if (day[i].ActArrivalTime.TimeOfDay >= _arrivalTime)
                    {
                        pairings.Add((day[i - 1], day[i]));
                        break;
                    }
                }
            }

            return pairings;
        }

        /// <summary>
        /// Calculates the dwell time of a bus for a particular timetable record.
        /// </summary>
        /// <param name="record">An historical timetable record to calculate the dwell from.</param>
        /// <returns>The time the service was dwelling for at the stop.</returns>
        private static TimeSpan GetDwellTime(IBusSolidHistoricTimeTable record)
        {
            //If it was a timing point then there are laws on how early a bus can leave.
            //Which means it might be artificially waiting when not needed. 
            if (record.IsTimingPoint)
            {
                //If a bus arrives Later then it was expected to.
                if (record.ActArrivalTime >= record.SchArrivalTime)
                {
                    //Time between what it actually arrived and when it actually departed. 
                    return (record.ActDepartureTime - record.ActArrivalTime);
                }
                
                
                //If a bus left earlier than it was allowed to, return zero.
                //As the bus shouldn't have been allowed to do so.
                if ((record.ActDepartureTime - record.SchDepartureTime).TotalSeconds < 0)
                    return new TimeSpan(0);


                //If a bus arrives earlier than it was expected to.
                //Time between when it should have arrived, and it actually leaving.
                return (record.ActDepartureTime - record.SchArrivalTime);
            }

            //Not a timing point so no obligations to remain at the stop for any longer than required.
            return (record.ActDepartureTime - record.ActArrivalTime);
        }
    }
}
