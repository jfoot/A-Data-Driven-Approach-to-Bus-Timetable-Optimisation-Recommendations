// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search;
using Timetable_Optimisation_Recommendations.Windows;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// Used to display the moves that have been made by the search algorithm after it has completed
    /// and displays the finalized timetables. 
    /// </summary>
    public partial class MovesMade : Page
    {
        ///<value>Contains the final best solution found by the search algorithm.</value>
        private readonly Solution _solution;
        ///<value>Contains a list of all the moves that have been made.</value>
        private readonly List<(Move moves, int score)> _movesMade;
        ///<value>The iteration the best move was found on.</value>
        private readonly int _iterationOfBestMove;

        ///<value>Contains the original start solution.</value>
        private readonly Solution _startSolution;


        /// <summary>
        /// The default constructor for the results page. 
        /// </summary>
        /// <param name="startSolution">The original input solution.</param>
        /// <param name="solution">The best solution found.</param>
        /// <param name="movesMade">A list of moves that were made.</param>
        /// <param name="iterationOfBestMove">The iteration count where the best move was found.</param>
        public MovesMade(Solution startSolution, Solution solution, int iterationOfBestMove, List<(Move moves, int score)> movesMade)
        {
            InitializeComponent();
            _solution = solution;
            _startSolution = startSolution;
            _movesMade = movesMade;
            _iterationOfBestMove = iterationOfBestMove;
        }

        /// <summary>
        /// Updates the GUI and lists of the moves that were made.
        /// </summary>
        private void DisplayMovesMade()
        {
            string summaryText = "";

            
            //For each move add to the string.
            for (int i = 0; i < _movesMade.Count; i++)
            {
                summaryText += _movesMade[i].moves + "      Score - " + _movesMade[i].score + Environment.NewLine;
               
                if (i == _iterationOfBestMove - 1)
                    summaryText += " *** The best solution was found above, at iteration " + _iterationOfBestMove + ", all subsequent moves were non-improving. This solution is "+ PercentageChange((int)_startSolution.ObjectiveFunctionValue(), (int)_solution.ObjectiveFunctionValue()) + "% improved on the original ***" + Environment.NewLine; 
            }

            MovesMadeTxt.Text = summaryText;
        }




        private Move[] GetMovesArray()
        {
            List<Move> moves = new(_movesMade.Count);

            foreach ((Move move, int _) in _movesMade)
                moves.Add(move);


            return moves.ToArray();
        }



        /// <summary>
        /// Called upon when the page has finished loading, displays all of the timetables generated. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadedUp(object sender, RoutedEventArgs e)
        {
            //Removes the last page from the history, which was the evaluator stage, while they could go here they probably don't want to.
            NavigationService? ns = NavigationService.GetNavigationService(this);
           
            //Prevent user from being able to go back at all.
            while (ns?.CanGoBack == true)
                ns.RemoveBackEntry();
            
            //Update the GUI.
            DisplayMovesMade();
            
            //Goes through every service in the solution and then displays the timetable in a new window.
            //This can be a bit slow due to how much data is being displayed.
            foreach ((IBusService service, Timetable_Evaluator.BlamedBusTimeTable[] timeTable) in _solution.BusTimeTables)
            {
                ViewTimetableHighlighted timetable = new(timeTable, service, PercentageChange(_startSolution.ScoreOfService(service), _solution.ScoreOfService(service)), GetMovesArray());
                timetable.Show();
            }
        }

        private static string PercentageChange(int originalValue, int newValue)
        {
            return ((originalValue - newValue) / (double)originalValue * 100.0).ToString("0.0");
        }


        private void ReturnClick(object sender, RoutedEventArgs e)
        {
            NavigationService? ns = NavigationService.GetNavigationService(this);
            ns?.Navigate(new MainPage());
        }
    }
}
