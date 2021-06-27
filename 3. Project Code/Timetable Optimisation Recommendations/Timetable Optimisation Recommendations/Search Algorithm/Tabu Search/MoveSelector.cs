// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search
{
    /// <summary>
    /// The move-selector is used to select the best move out of an array of moves,
    /// the neighborhood, to generate a new solution.
    /// </summary>
    public class MoveSelector
    {
        ///<value>Stores a reference to the main evaluator object.</value>
        private readonly TimeTableEvaluator _evaluator;

        /// <summary>
        /// Default constructor, takes in a reference to the evaluator object.
        /// </summary>
        /// <param name="evaluator"></param>
        public MoveSelector(TimeTableEvaluator evaluator)
        {
            _evaluator = evaluator;
        }
        
        /// <summary>
        /// Given an array of moves, identify which is the best move and return the
        /// new solution with that move applied to it.
        /// </summary>
        /// <param name="moves">An array of possible moves to make.</param>
        /// <param name="progress">Used to report back to the GUI or listener of the progress of this task.</param>
        /// <returns>
        /// The best solution which is the best move applied to the current solution.
        /// Along with the selected move that got us there.
        /// </returns>
        public async Task<(Solution, Move)> BestMoveSelectorAsync(Move[] moves, TabuList tabuList, IProgress<AdvancedProgressReporting>? progress = null)
        {
            //All of this code is used for the progress bar.
            int totalCompletedTasks = 0;
            int totalTasks = moves.Length;

            Progress<ProgressReporting> subProgress = new();
            subProgress.ProgressChanged += delegate (object? o, ProgressReporting d)
            {
                progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0 + (1.0 / totalTasks * d.Value),
                    d.Value, d.Message));
            };

            progress?.Report(new AdvancedProgressReporting("Stage 3 : Evaluating Moves", 3));

            SlackTimeEvaluator slack = new(_evaluator);
            ServiceCohesionEvaluator cohesion = new(_evaluator);

            //Keeps track of the best move out of all the moves.
            (Solution solution, Move move)? bestSolution = null;

            //For each move evaluate it.
            foreach (Move move in moves)
            {
                //This shouldn't be needed as it was checked previously in the neighborhood generator but to be safe.
                if(tabuList.IsTabu(move))
                    continue;

                //Apply the move to the current solution 
                Solution tempSolution = _evaluator.CurrentSolution.ReplaceMove(move);
                
                //Evaluate the proposed solution.
                await slack.FindSingleBlameSlackTime(tempSolution.BusTimeTables[move.Service], subProgress);
                //Re-normalizes the specific part of the solution.
                SlackTimeEvaluator.StandardiseSolution(tempSolution);

                //This will be re-calculate everything and also re-normalize it for us.
                cohesion.FindBlameServiceCohesion(tempSolution);
                tempSolution.CalculateTotalBlames();

                //If the best move isn't set yet or it is better (lower value is better) set equal to best solution.
                if (bestSolution == null || ObjectiveFunctionChangeAmountAdjusted(tempSolution,move) < ObjectiveFunctionChangeAmountAdjusted(bestSolution.Value.solution,bestSolution.Value.move))
                    bestSolution = (tempSolution,move);

                //Increment the number of completed tasks and report to the GUI.
                ++totalCompletedTasks;
                progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0,
                    100.0, "Finished Evaluating Move " + totalCompletedTasks));
            }

            //If a move was found then set it as tabu.
            if(bestSolution.HasValue)
                tabuList.SetTabu(bestSolution.Value.move);
            

            //Return the new best solution, should only ever return null if moves was of length zero.
            return bestSolution ?? throw new NullReferenceException("No best solution could be found, were no moves provided?");
        }

        /// <summary>
        /// Used to ensure that the change amount isn't too great.
        /// </summary>
        /// <param name="solution">The current solution.</param>
        /// <param name="move">The move being applied to the solution.</param>
        /// <returns>The objective function score adjusted for how much of a change is happening.</returns>
        private static double ObjectiveFunctionChangeAmountAdjusted(Solution solution, Move move)
        {
            //If more than 15min of change is being made then increase score by 150% (higher score is worse)
            //So it is very unlikely to ever go for this.
            if (move.ChangeAmount >= 15)
            {
                return solution.ObjectiveFunctionValue() * 1.5;
            }
            else if (move.ChangeAmount >= 10)
            {
                return solution.ObjectiveFunctionValue() * 1.2;
            }
            else if (move.ChangeAmount >= 5)
            {
                return solution.ObjectiveFunctionValue() * 1.1;
            }

            //If less than 5 min of changes are being suggested just allow the move.
            return solution.ObjectiveFunctionValue();
        }
    }
}
