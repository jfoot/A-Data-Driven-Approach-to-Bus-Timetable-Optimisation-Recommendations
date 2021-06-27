// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search
{
    /// <summary>
    /// Used to represent a single solution to the problem, the actual solution is stored in a dictionary, where the service is
    /// the key and an array of timetable records is the value.
    /// </summary>
    public class Solution : ICloneable
    {
        /// <summary>
        /// Stores the solution, as a set of timetable records for a day with an associated service. 
        /// </summary>
        public Dictionary<IBusService, BlamedBusTimeTable[]> BusTimeTables { get; private set; }

        /// <summary>
        /// Default constructor takes in a solution dictionary and stores it.
        /// </summary>
        /// <param name="busTimetables"></param>
        public Solution(Dictionary<IBusService, BlamedBusTimeTable[]> busTimetables)
        {
            BusTimeTables = busTimetables;
        }

        /// <summary>
        /// Returns back an objective function value for the current solution, a lower score is better.
        /// Values can only be compared against solutions to the same problem. These are not standardised
        /// between different searches.  
        /// </summary>
        /// <returns>An object function value, lower the score the better the solution. Zero being the "perfect"
        /// timetable.
        /// </returns>
        public double ObjectiveFunctionValue()
        {
            //This no longer makes any account for cohesion weights.
            //The sum of the absolute value of the weights. 
            return BusTimeTables.Values.Sum(timeTable => 
                timeTable.Sum(record => Math.Abs(record.SlackWeights.RawWeight ?? 0)));
        }

        /// <summary>
        /// Goes through every record in the solution space and updates their weights to the new total weight.
        /// </summary>
        public void CalculateTotalBlames()
        {
            foreach ((IBusService _, BlamedBusTimeTable[] records) in BusTimeTables)
                foreach (BlamedBusTimeTable record in records)
                    record.UpdateTotalWeight();
        }

        /// <summary>
        /// Creates a deep-clone of thew current solution and then replaces a move in the solution space.
        /// Returns the copy of the solution with the altered move.
        /// </summary>
        /// <param name="move">The move to replace the service with.</param>
        /// <returns></returns>
        public Solution ReplaceMove(Move move)
        {
            Solution temp = (Solution)Clone();
            temp.BusTimeTables[move.Service] = move.TimeTable;
            return temp;
        }


        /// <summary>
        /// Returns the objective function score of in single service in the solution.
        /// This can be used to work out how much one service has improved. 
        /// </summary>
        /// <param name="service">The service in the solution to generate a score for. If not in the solution 0.</param>
        /// <returns>Objective score of one service in the solution.</returns>
        public int ScoreOfService(IBusService service)
        {
            if (BusTimeTables.ContainsKey(service))
                 return  (int)BusTimeTables[service].Sum(record => Math.Abs(record.SlackWeights.RawWeight ?? 0));

            return 0;
        }

        /// <summary>
        /// Creates a deep-clone of the object, this will shallow-clone the object and then deep-clone the bus-timetable solution dictionary. 
        /// </summary>
        /// <returns>A deep-clone of "this" object.</returns>
        public object Clone()
        {
            //Shallow Clones first.
            Solution temp = (Solution) MemberwiseClone();
            //Performs a deep-clone of the dictionary. 
            temp.BusTimeTables = BusTimeTables.ToDictionary(entry => entry.Key, entry => entry.Value.Select(a => (BlamedBusTimeTable)a.Clone()).ToArray());
            return temp;
        }
    }
}
