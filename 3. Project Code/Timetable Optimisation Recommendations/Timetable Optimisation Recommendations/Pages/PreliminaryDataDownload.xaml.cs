// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Route_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// The preliminary data download lets you download all of the data files that you are going to need for the search.
    /// It also lets you see the services past performance. 
    /// </summary>
    public partial class PreliminaryDataDownload : Page
    {
        /// <value>Contains all of the logic for the pre-evaluator downloading and checks.</value>
        private readonly PreEvaluatorChecks _evaluator;

        ///<value>Used to bind and update the GUI progress to the user.</value>
        public AdvancedProgressReporting Reporter { get; } = new();

        /// <summary>
        /// The default constructor, takes in the information from the previous pages.
        /// </summary>
        /// <param name="cluster">The dates that the user is requesting data for.</param>
        /// <param name="collection">All of the shared route-segments containing information on other services we also want data for.</param>
        public PreliminaryDataDownload(Cluster cluster, RouteSegmentCollection collection)
        {
            InitializeComponent();
            _evaluator = new PreEvaluatorChecks(cluster, collection);
        }

        /// <summary>
        /// As soon as the page has loaded up begin the task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadedUp(object sender, RoutedEventArgs e)
        {
            //Prevent the user from stopping the task once started.
            ShowsNavigationUI = false;

            Progress<AdvancedProgressReporting> progressObj = new();
            progressObj.ProgressChanged += delegate (object? o, AdvancedProgressReporting d)
            {
                //Update to the user the progress of the task.
                Window? window = Window.GetWindow(this);
                if (window != null)
                    window.TaskbarItemInfo.ProgressValue = d.Value / 100.0;
                Reporter.Update(d);
                LoadingText.ScrollToEnd();
            };

            //Start downloading everything required.
            await _evaluator.DownloadAllFilesNeeded(progressObj);

            //As soon as completed navigate off the page.
            ShowsNavigationUI = true;
            NavigationService ns = NavigationService.GetNavigationService(this)!;
            ns.Navigate(new PreviousPerformance(_evaluator));
        }
    }
}
