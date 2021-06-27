// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Route_Analyser;
using Timetable_Optimisation_Recommendations.Timetable_Analyser;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// The route-segment selector page finds other services that share a common route-segment with the primary service.
    /// And then allows the user to accept secondary services to also optimism their timetables for.
    /// </summary>
    public partial class RouteSegmentSelector : Page
    {
        ///<value>Stores information on the dates for data.</value>
        private readonly Cluster _cluster;

        ///<value>Stores information on the other services that hare route segments.</value>
        public RouteSegmentCollection Collection { get; } = new();

        ///<value>Used to update the GUI and report progress.</value>
        public ProgressReporting Reporter { get; } = new();


        /// <summary>
        /// The default constructor, takes in the dates from the other services. 
        /// </summary>
        /// <param name="cluster">Dates of interest for the search.</param>
        public RouteSegmentSelector(Cluster cluster)
        {
            InitializeComponent();
            _cluster = cluster;
        }

 
        /// <summary>
        /// Upon loading up the page immediately try to start finding shared common route segments.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadedUp(object sender, RoutedEventArgs e)
        {
            //Prevent the user from stopping the task once started.
            ShowsNavigationUI = false;

            Progress<ProgressReporting> progressObj = new();
            progressObj.ProgressChanged += delegate (object? o, ProgressReporting d)
            {
                //Update to the user the progress of the task.
                Window? window = Window.GetWindow(this);
                if (window != null)
                    window.TaskbarItemInfo.ProgressValue = d.Value / 100.0;
                Reporter.Update(d);
                loadingText.ScrollToEnd();
            };

            //Starts finding shared route segments.
            RouteSegmentFinder finder = new(_cluster.GetAssociatedService());
            await Collection.InitialiseAsync(finder, progressObj);
            NextButton.IsEnabled = true;
            ShowsNavigationUI = true;
        }

        /// <summary>
        /// Called upon when a user adds a service to the search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            //If the value is null or otherwise not in the correct format.
            if (excludedService.SelectedItem is not IBusService service)
                return;

            await Collection.AddService(service, null);
        }

        /// <summary>
        /// Called upon when the user removes a service from the search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (includedService.SelectedItem is not IBusService service)
                return;

            //The user is not allowed to remove the primary service.
            if (service == _cluster.GetAssociatedService())
                MessageBox.Show("Cannot remove the primary service from the list of services.");
            else
                Collection.RemoveServiceAsync(service);
        }

        /// <summary>
        /// Called upon when the user has finished selecting the services that they wish to optimism for.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartOptimisation(object sender, RoutedEventArgs e)
        {
           


            //Starts the pre-evaluator checks.
            NavigationService ns = NavigationService.GetNavigationService(this)!;
            ns.Navigate(new PreliminaryDataDownload(_cluster, Collection));
        }
    }
}
