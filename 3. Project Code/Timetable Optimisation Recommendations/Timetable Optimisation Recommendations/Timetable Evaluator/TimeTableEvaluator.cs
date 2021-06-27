// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Route_Analyser;
using Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{
    /// <summary>
    /// The main evaluator entry point, give it a set of dates where you want to use data from, an initial starting solution
    /// and a route-segment collection and then it can improve upon the timetable. 
    /// </summary>
    public class TimeTableEvaluator
    {
        ///<value>Dates when the timetables where the same in the year.</value>
        public DateTime[] RelatedDates { get; }

        ///<value>The current proposed solution.</value>
        public Solution CurrentSolution { get; private set; }
        
        ///<value>The initial start solution.</value>
        public Solution? StartSolution { get; private set; }

        ///<value>The best solution found in any iteration</value>
        public (Solution? Solution, int Iteration) BestSolution { get; private set; } = (null, 0);

        ///<value>Used to store a list of moves that were made to the timetable.</value>
        public List<(Move, int)> MovesMade { get; } = new();

        ///<value>Stores information about shared route-segment collections. </value>
        public RouteSegmentCollection Collection { get; }

        /// <value>Keeps track of what moves are tabu or not.</value>
        private readonly TabuList _tabuList = new();

        ///<value>Used to evaluate a services cohesion between each-other at shared route-segments.</value>
        private readonly ServiceCohesionEvaluator _cohesionEvaluator;
        ///<value>Used to evaluate the excess slack time of a service.</value>
        private readonly SlackTimeEvaluator _slackTimeEvaluator;
        ///<value>Used to generate a neighborhood of solutions</value>
        private readonly NeighbourhoodSolution _neighbourhoodSolutionGenerator;
        ///<value>Used to select the best move out of the neighborhood</value>
        private readonly MoveSelector _moveSelector;

        ///<value>Used to keep track of what iteration the search is on.</value>
        private int _iterationCount = 1;


        /// <summary>
        /// The default constructor for the main evaluator.
        /// </summary>
        /// <param name="relatedDates">A set of dates where the timetables were the same and to look for data.</param>
        /// <param name="segmentCollection">A route-segment collection, to find the common-shared path segments.</param>
        /// <param name="busTimeTables">The current timetable/ initial proposed solution.</param>
        public TimeTableEvaluator(DateTime[] relatedDates, RouteSegmentCollection segmentCollection,
            Dictionary<IBusService, IBusTimeTable[]> busTimeTables)
        {
            RelatedDates = relatedDates;
            Collection = segmentCollection;
            CurrentSolution = new Solution(ConvertToBlameRecords(busTimeTables));
            _cohesionEvaluator = new ServiceCohesionEvaluator(this);
            _slackTimeEvaluator = new SlackTimeEvaluator(this);
            _neighbourhoodSolutionGenerator = new NeighbourhoodSolution(this);
            _moveSelector = new MoveSelector(this);
        }

        /// <summary>
        /// Used to tell what the last solution score was and to show if it has been improving or not.
        /// </summary>
        /// <returns>
        /// A string to say the score/progress of the algorithm, along with a boolean value.
        /// True - Has improved since last move.
        /// False- Gotten worse since last move.
        /// </returns>
        public (string, bool) GetCurrentScoreString()
        {
            //If we've made at least two moves, i.e we can compare it against the previous move. 
            if (MovesMade.Count >= 2)
            {
                //If the most recent score is lower than the last one (i.e it has gotten better)
                if (MovesMade[^1].Item2 <= MovesMade[^2].Item2)
                {
                    return _iterationCount - 1  == BestSolution.Iteration ? (MovesMade[^1].Item2 + " ▲ PB", true) : (MovesMade[^1].Item2 + " ▲", true);
                }
                //Else it has gotten worse!
                else
                {
                    return (MovesMade[^1].Item2 + " ▼ (" + (_iterationCount - BestSolution.Iteration - 1) + " Moves)", false);
                }
            }

            //If we've made one move just show last score.
            if (MovesMade.Count == 1)
                return (MovesMade[0].Item2.ToString(),true);

            //Else very first move nothing to report. 
            return ("Generating...",true);
        }


        /// <summary>
        /// Converts the initial proposed solution and converts it into a blamed timetable dictionary array,
        /// such that it can be reasoned with and weights applied to the records.
        /// </summary>
        /// <param name="busTimeTables">An input IBusTimetable Dictionary.</param>
        /// <returns>An output BlamedBusTimetable Dictionary.</returns>
        private static Dictionary<IBusService, BlamedBusTimeTable[]> ConvertToBlameRecords(Dictionary<IBusService, IBusTimeTable[]> busTimeTables)
        {
            Dictionary<IBusService, BlamedBusTimeTable[]> temp = new();

            //For each key pair value, convert it and add it to the new dictionary. 
            foreach ((IBusService busService, IBusTimeTable[] records) in busTimeTables)
                temp.Add(busService, Array.ConvertAll(records, record => new BlamedBusTimeTable(record)));
            
            return temp;
        }


       
        /// <summary>
        /// Performs one iteration of the search algorithm, once it's completed the solution will have changed by one move.
        /// </summary>
        /// <returns>
        /// Updates the solution by one move improving the solution. Returns true or false for if you can perform
        /// another subsequent move.
        /// </returns>
        public async Task<bool> PerformIterationAsync(IProgress<AdvancedProgressReporting>? progress = null)
        {
            //Evaluate the current solution, finding high-risk areas. 
            await FindTotalBlameAsync(progress);

            //Generates a set of moves that can be performed from the current position which produces a new solution.
            Move[] moves = await _neighbourhoodSolutionGenerator.GenerateNegibourhood(_tabuList, progress);

            //If no moves could be found, e.g a tabu-tenure was too long or the search space to small.
            //Very very unlikely to ever actually happen, but check just to be safe.
            if (moves.Length == 0)
            {
                MessageBoxResult result = MessageBox.Show("No valid moves found, is your tabu-tenure value set to high for the size of the search space?" + Environment.NewLine + "Would you like to stop the search here (yes)? Else no, by un-tabuing previous areas of the search space early.", "No Moves Found", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                //Stop the search early.
                if (result == MessageBoxResult.Yes)
                {
                    return false;
                }
                //Un-tabu some parts of the search space. 
                else
                {
                    _tabuList.FreeUpTabuListEarly();
                    return true;
                }
            }
            
            
            //Else moves found so use them.
            //Work out the best move from the neighborhood and select it.
            (Solution solution, Move move) = await _moveSelector.BestMoveSelectorAsync(moves, _tabuList, progress);

            //Updates to the new latest solution.
            CurrentSolution = solution;

            //If the new current solution is better than our best solution then set it to be the best value. 
            if (BestSolution.Solution == null || CurrentSolution.ObjectiveFunctionValue() < BestSolution.Solution?.ObjectiveFunctionValue())
                BestSolution = (CurrentSolution, _iterationCount);

            //Add the move made to the list of moves selected.
            MovesMade.Add((move, (int)solution.ObjectiveFunctionValue()));


            progress?.Report(new AdvancedProgressReporting(100, 100, Environment.NewLine + "Iteration Completed Move Made- New Score : " + (int)CurrentSolution.ObjectiveFunctionValue() + Environment.NewLine + move + Environment.NewLine));
            _iterationCount++;

            //Let it know this iteration was successful and another iteration can be performed.
            return true;
        }


        /// <summary>
        /// Works out the blame values for all of the records. This identifies the high-issue areas.
        /// Such that a neighborhood can be generated and moves formulated. 
        /// </summary>
        /// <returns>Adds (or updates) the blame values for all of the records.</returns>
        private async Task FindTotalBlameAsync(IProgress<AdvancedProgressReporting>? progress = null)
        {
            progress?.Report(new AdvancedProgressReporting("Stage 1 : Finding Problem Areas", 1));

            //Calculate the slack time blame.
            if (_iterationCount == 1)
                await _slackTimeEvaluator.FindBlameSlackTime(CurrentSolution, progress);
                
            
            //Calculate the cohesion blame
            _cohesionEvaluator.FindBlameServiceCohesion(CurrentSolution);
            progress?.Report(new AdvancedProgressReporting(10, 100, "Completed evaluation of service cohesion."));

            //Calculate the total blame.
            CurrentSolution.CalculateTotalBlames();
            
            //If it is the first solution that has now been "blamed" keep a reference.
            if(_iterationCount == 1)
                StartSolution = (Solution)CurrentSolution.Clone();

            progress?.Report(new AdvancedProgressReporting(100, 100, "Completed finding high-risk problem areas."));
        }
    }
}
