// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// The actual main evaluator stage of the program, this is the GUI for the "main part".
    /// But the page mainly contains the GUI to let the user know of the progress.
    /// </summary>
    public partial class Evaluator : Page
    {
        /// <value>Used to report back to GUI the process progress.</value>
        public AdvancedProgressReporting Reporter { get; } = new();

        ///<value>Contains the actual logic and implementation of the evaluator.</value>
        private readonly TimeTableEvaluator _evaluator;

        ///<value>Keeps track of how many iterations the search algorithm has performed.</value>
        private int _iteration = 1;


        private bool _stopping = false;

        /// <summary>
        /// The default evaluator constructor, takes in the information from the pre-evaluator.
        /// </summary>
        /// <param name="preEvaluator">The pre-evaluator object</param>
        public Evaluator(PreEvaluatorChecks preEvaluator)
        {
            InitializeComponent();
            _evaluator = preEvaluator.EvaluateTimeTable();
        }


        /// <summary>
        /// Called upon when the window has finished loading, then start the evaluator straight away.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadedUpAsync(object sender, RoutedEventArgs e)
        {
            //Prevent the user from stopping the task once started.
            ShowsNavigationUI = false;

            //Used to update the GUI.
            Progress<AdvancedProgressReporting> progressObj = new();
            progressObj.ProgressChanged += delegate (object? o, AdvancedProgressReporting d)
            {
                Reporter.Update(d);

                //Update to the user the progress of the task.
                Window? window = Window.GetWindow(this);
                if (window != null)
                    window.TaskbarItemInfo.ProgressValue = ((_iteration - 1) / (double)Properties.Settings.Default.IterationLimit)
                                                       + ((Reporter.StageVal / 3.0) * (1 / (double)Properties.Settings.Default.IterationLimit));
                loadingText.ScrollToEnd();
            };


            //Perform several iterations of the search algorithm as defined by the user.
            for (int i = 0; i < Properties.Settings.Default.IterationLimit; i++)
            {
                Iteration.Content = "Iteration " + _iteration + " of " + Properties.Settings.Default.IterationLimit;

                (string message, bool isImproving) = _evaluator.GetCurrentScoreString();

                Score.Content = "Score : " + message;
                if (_iteration > 2)
                    Score.Foreground = isImproving ? Brushes.LightGreen : Brushes.Red;

                //If the search cannot continue any further then break out of it.
                if (!await _evaluator.PerformIterationAsync(progressObj))
                    break;

                //Clear out the reporting logs after every iteration.
                loadingText.Clear();
                Reporter.Clear();
                _iteration++;

                if (_stopping)
                    break;
            }

            ShowsNavigationUI = true;
            //Once completed show the changes made.
            NavigationService ns = NavigationService.GetNavigationService(this)!;
            ns.Navigate(new MovesMade(_evaluator.StartSolution ?? _evaluator.CurrentSolution,_evaluator.BestSolution.Solution ?? _evaluator.CurrentSolution, _evaluator.BestSolution.Iteration, _evaluator.MovesMade));
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
           MessageBoxResult result =  MessageBox.Show("Are you sure you wish to stop the search early?", "Stop Early?", MessageBoxButton.YesNo);

           if (result == MessageBoxResult.Yes)
           {
               _stopping = true;
               StopButton.IsEnabled = false;
               StopButton.Content = "Stopping...";
           }
        }
    }
}
