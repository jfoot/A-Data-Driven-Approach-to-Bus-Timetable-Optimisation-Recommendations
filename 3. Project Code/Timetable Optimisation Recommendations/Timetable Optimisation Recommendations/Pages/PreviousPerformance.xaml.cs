// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;
using Timetable_Optimisation_Recommendations.Timetable_Performance_Evaluator;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// Once all of the data has been downloaded in the pre-evaluator checks show the performance metrics to the user.
    /// This is the final stage before starting the actual search.
    /// </summary>
    public partial class PreviousPerformance : Page
    {
        ///<value>Used to store the list of services and their performance metrics.</value>
        public ObservableCollection<LatenessReport> ServiceCardCollection { get; } = new();

        ///<value>Used to store the results of the pre-evaluator to give it to the evaluator next.</value>
        private readonly PreEvaluatorChecks _preEvualtor;

        /// <summary>
        /// The default constructor to the page, takes in the pre-evaluator results 
        /// </summary>
        /// <param name="preEvaluator">The pre-evaluator results from the previous page.</param>
        public PreviousPerformance(PreEvaluatorChecks preEvaluator)
        {
            InitializeComponent();

            _preEvualtor = preEvaluator;

            //For each of the services add the lateness records to the ObservableCollection to update the GUI.
            foreach (LatenessReport report in preEvaluator.PerformanceEvaluator.ServiceLatenessReports)
                ServiceCardCollection.Add(report);
        
        }

        /// <summary>
        /// Called upon when the user actually wants to start the search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartSearch(object sender, RoutedEventArgs e)
        {
            NavigationService ns = NavigationService.GetNavigationService(this)!;
            ns.Navigate(new Evaluator(_preEvualtor));
        }

        /// <summary>
        /// Called upon when the page has finished loading, it then removes the previous page from history (the pre-evaluator).
        /// This is because the user has no reason to go back to that page, as it as an intermediary stage.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadedUp(object sender, RoutedEventArgs e)
        {
            NavigationService? ns = NavigationService.GetNavigationService(this);
            ns?.RemoveBackEntry();
        }
    }
}
