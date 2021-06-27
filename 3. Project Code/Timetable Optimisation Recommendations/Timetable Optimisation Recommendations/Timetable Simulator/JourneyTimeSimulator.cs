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
    /// The journey time simulator class takes in a theoretical departure time, two stops to travel between and some other information.
    /// It then estimates how long it would likely take to travel between the two stops at this time of day.
    /// </summary>
    public class JourneyTimeSimulator : TimeSimulator
    {
        /// <summary>
        /// The departure or arrival time of interest, this is the new theoretical time for a service at the first stop.
        /// </summary>
        private readonly TimeSpan _targetTime;
        /// <summary>
        /// The start stop that a service leaves from.
        /// </summary>
        private readonly IBusStop _start;
        /// <summary>
        /// The end stop that a service arrives at.
        /// </summary>
        private readonly IBusStop _end;
        /// <summary>
        /// A list of known services that go between the two stops consecutively/ without any stops in between.
        /// </summary>
        private readonly IBusService[] _services;
        /// <summary>
        /// Dates for which the timetables were similar, so that we can request data for parts of the year which had a similar 
        /// service and travel demands as the date were looking for.
        /// </summary>
        private readonly DateTime[] _dates;


        private readonly bool _isForwardProp;

        /// <summary>
        /// Default constructor for the object, takes in all the required parameters. 
        /// </summary>
        /// <param name="targetTime">The departure or arrival time of interest at the start stop. Dependent upon the direction of propagation.</param>
        /// <param name="start">The start stop.</param>
        /// <param name="end">The end stop.</param>
        /// <param name="services">A list of services that are known to go between the start and end stop consecutively.</param>
        /// <param name="dates">A list of dates for when the timetables where the same.</param>
        /// <param name="isForwardProp">Is Forward propagating time, default true, else backwards</param>
        public JourneyTimeSimulator(TimeSpan targetTime, IBusStop start, IBusStop end, IBusService[] services,
            DateTime[] dates, bool isForwardProp = true)
        {
            _targetTime = targetTime;
            _start = start;
            _end = end;
            _services = services;
            _dates = dates;
            _isForwardProp = isForwardProp;
        }

        /// <summary>
        /// Calculates the estimated journey time, at the specific date and time given between two stops. 
        /// </summary>
        /// <param name="progress">Used to feed-back to the GUI the amount of progress made on the simulator.</param>
        /// <returns>The amount of time it would take to journey between the two stops.</returns>
        /// <remarks>If returns 0, there is no data to make an estimate, which would indicate no route goes between these two stops consecutively.</remarks>
        public async Task<TimeSpan> ProduceEstimatedTravelTimes(IProgress<double>? progress = null)
        {
            //Used to record the progress of a single task.
            var progressIndividual = new Progress<double>();

            //Stores a list of estimated journey durations/times along with their predicted accuracy. 
            List<(TimeSpan duration, double accuracyWeight)> journeyTimes = new();

            //For each bus service that we know goes between these two stops.
            for (int i = 0; i < _services.Length; i++)
            {
                //Calculates the overall progress and the process of the current task and passes it back to main.
                progressIndividual.ProgressChanged += delegate (object? o, double d)
                {
                    progress?.Report(((i / (double)_services.Length) + (d / 100.0 * 1.0 / _services.Length)) * 100);
                };

                //Get an array of all the bus timetables at every date of interest given.
                IBusSolidHistoricTimeTable[][] data = await TimetableRetrieval.GetHistoricTimeTableBatch(_dates, _services[i], progressIndividual);

                //Stores a list of tuples of journey records, the journey directly before and directly after our target time.
                List<(JourneyTime? j1, JourneyTime? j2)> dayPairings = new();

                //Go through each day of data.
                foreach (IBusSolidHistoricTimeTable[] day in data)
                {
                    //Get only the records in the day which are between the two stops of interest to us.
                    List<JourneyTime> dayRecords = FindRecordsBetweenStops(day);
                    //finds the two journey times of interest out of the above set.
                    dayPairings.Add(FindPairRecordsBetweenTimeOfInterest(dayRecords));
                }

                //Add the services set of estimated values, one value per day per service.
                journeyTimes.AddRange(GenerateWeightedDistribution(dayPairings));
            }

            //Generate the average of all the days and of all the services
            return GenerateWeightedAverage(journeyTimes);
        }

        /// <summary>
        /// Given an Historical Solid Timetable record and a Bus Stop, return if the record is about the bus stop or not.
        /// </summary>
        /// <param name="r1">Time table record.</param>
        /// <param name="s1">Bus stop.</param>
        /// <returns>True if the record pertains about the bus stop</returns>
        private static bool IsRecordStopSame(IBusSolidHistoricTimeTable r1, IBusStop s1)
        {
            return r1.Location.AtcoCode == s1.AtcoCode;
        }


        /// <summary>
        /// Given a set of time table records for day, find all the records that are between the Start and End stop. 
        /// These two stops must be consecutive and in the same order.
        /// </summary>
        /// <param name="records">The timetable records for one day.</param>
        /// <returns>A list of journey times, which is how long it took to travel between the two stops and what time of day this occurred. </returns>
        private List<JourneyTime> FindRecordsBetweenStops(IBusSolidHistoricTimeTable[] records)
        {
            List<JourneyTime> dayRecords = new();

            //Go through all the records in the day.
            for (int i = 1; i < records.Length; i++)
            {
                //Once you have found the first record that has a later actual arrival time than the time we are looking for, get that record and the one directly before.
                if (IsRecordStopSame(records[i - 1], _start) && IsRecordStopSame(records[i], _end))
                {
                    dayRecords.Add(new JourneyTime(records[i - 1], records[i], _isForwardProp));
                }
            }
            //Orders all values so that they are in time order for that single day.
            dayRecords = dayRecords.OrderBy(dayRecord => dayRecord.TimeOfInterest).ToList();

            return dayRecords;
        }


        /// <summary>
        /// Give a list of journeys between two stops at different points in the day, find the two records that are either side of our target time.
        /// The record directly before and the record directly after our target time.
        /// </summary>
        /// <param name="journeySegmenets">A list of journeys between two segments.</param>
        /// <returns>The record directly before, and the record directly after our target time.</returns>
        private (JourneyTime? j1, JourneyTime? j2) FindPairRecordsBetweenTimeOfInterest(List<JourneyTime> journeySegmenets)
        {
            //If the list is empty there is no data.
            if (journeySegmenets.Count == 0)
                return (null, null);

            //If only one record is ever found then you cannot make a pairing so return the same element.
            if (journeySegmenets.Count == 1)
                return (journeySegmenets[0], journeySegmenets[0]);


            //If the time of interest is before the earliest time record, then you will never find a pairing.
            if (_targetTime <= journeySegmenets[0].TimeOfInterest)
                return (journeySegmenets[0], journeySegmenets[0]);

            //If the time of interest is after the latest time record, then you will never find a pairing.
            if (_targetTime >= journeySegmenets.Last().TimeOfInterest)
                return (journeySegmenets.Last(), journeySegmenets.Last());



            //Go through all of the records that are only between the two stops of interest.
            for (int i = 1; i < journeySegmenets.Count; i++)
            {
                //Find the first record that is past the time we were looking for, take that value and the previous value, the first one before the time we were looking for.
                if (journeySegmenets[i].TimeOfInterest >= _targetTime)
                {
                    return (journeySegmenets[i - 1], journeySegmenets[i]);
                }
            }

            //Theoretically it shouldn't be possible to ever get here.
            throw new ApplicationException("Failed to find a pairing of interest between two stops.");
        }


        /// <summary>
        /// Given a list of pairings of journey times, generate a weighted time between the two journeys based upon the target time.
        /// Return this approximate time along with the minimum value either of the two records was to the target time. The smaller this value is
        /// the smaller the spread and the higher the accuracy of the score is likely to be. 
        /// </summary>
        /// <param name="dailyPairings">A list of tuples of Journey times, one journey before and one after our target time.</param>
        /// <returns>A list of tuples of estimated travel times, generated for a weighted average between the two times and the minimum time between the target and actual, which can be used as an accuracy score.</returns>
        private List<(TimeSpan, double)> GenerateWeightedDistribution(List<(JourneyTime? j1, JourneyTime? j2)> dailyPairings)
        {

            List<(TimeSpan duration, double accuracyWeight)> weightedJourneyTimes = new();

            //Go through every days worth of pairs between the target time. and calculate the weighted average.
            foreach ((JourneyTime? j1, JourneyTime? j2) in dailyPairings)
            {
                //If either value is null skip it.
                if (j1 == null || j2 == null)
                    continue;

                //The journey duration of the two journeys.
                TimeSpan t1 = j1.TravelTime;
                TimeSpan t2 = j2.TravelTime;

                // The bigger the gap, the less reliable it is, so the smaller the weight, so we flip D1 to go with T2 and vice versa.
                //Time between R1 arriving and our goal.
                TimeSpan d1 = _targetTime - j1.TimeOfInterest;
                //Time between R2 arriving and our goal.
                TimeSpan d2 = j2.TimeOfInterest - _targetTime;

                //Time difference between R1 and R2
                TimeSpan totalDifference = (j2.TimeOfInterest - j1.TimeOfInterest);


                //If item 1 and Item 2 are the same value, which happens if we are asking for a time earlier or later than a pairing could be found, then just work out time difference.
                //Else item 1 and item 2 are different, so generate weighted average of result.
                //The lower of the two values, means it is closer to the target time, so higher accuracy, so we inverse it for a higher weight. 
                weightedJourneyTimes.Add(totalDifference.TotalSeconds == 0
                    ? (j1.TravelTime, CalculateInverseWeight(d1.TotalSeconds))
                    : (t1 * (d2 / totalDifference) + t2 * (d1 / totalDifference), CalculateInverseWeight(d1.TotalSeconds, d2.TotalSeconds)));
            }

            //The list of estimated journey times along with it's accuracy measure. 
            return weightedJourneyTimes;
        }

    }
}
