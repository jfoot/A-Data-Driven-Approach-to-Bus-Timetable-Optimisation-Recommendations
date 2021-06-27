// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Timetable_Analyser;

namespace Timetable_Optimisation_Recommendations.Pages
{
    /// <summary>
    /// The date selector page, this is where the user is asked to input a a date range, for which they intend upon making
    /// a new timetable for.
    /// </summary>
    public partial class DateSelector : Page 
    {
        /// <value>The Start date for which they wish to get timetable data for.</value>
        public DateTime StartDate { get; set; } = DateTime.Today;
        ///<value>The end date for which they wish to get timetable data for.</value>
        public DateTime EndDate { get; set; } = DateTime.Today;

        ///<value>The service for which they are finding timetables groups for.</value>
        public IBusService Service { get; }

        ///<value>Used to store when different timetables were in affect.</value>
        public ObservableCollection<Cluster> TimeTableClusters { get; } = new();

        ///<value>Used to update the GUI progress bar.</value>
        public ProgressReporting Reporter { get; } = new();

      
        /// <summary>
        /// The default constructor, takes in a service ID string and then creates the date selector for that service.
        /// </summary>
        /// <param name="serviceId"></param>
        public DateSelector(string serviceId)
        {
            InitializeComponent();
            Service = BusOperatorFactory.Instance.Operator.GetService(serviceId);
        }


        /// <summary>
        /// Called upon when the user presses the search button, begins actually searching for periods when the
        /// timetable changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SearchForGroupings(object sender, RoutedEventArgs e)
        {
            //If the user has given a valid start and end date.
            if (IsValid())
            {
                //Prevent the user from clicking on it again until it is completed.
                SearchButton.IsEnabled = false;
                ShowsNavigationUI = false;

                //Creates the grouper.
                TimeTableGrouper grp = new(Service);
                TimeTableClusters.Clear();
                Reporter.Clear();

                Progress<ProgressReporting> progressObj = new();
                progressObj.ProgressChanged += delegate(object? o, ProgressReporting d)
                {
                    //Update to the user the progress of the task.
                    Window? window = Window.GetWindow(this);
                    if (window != null)
                        window.TaskbarItemInfo.ProgressValue = d.Value / 100.0;

                    Reporter.Update(d);
                    LoadingText.ScrollToEnd();
                };

                //Adds the values into the observable collection
                Array.ForEach(await grp.FindGroupings(progressObj, StartDate, EndDate), cluster => TimeTableClusters.Add(cluster));

                SearchButton.IsEnabled = true;
                ShowsNavigationUI = true;
            }
        }

        /// <summary>
        /// Ensures that the user has selected a valid start and end date.
        /// </summary>
        /// <returns>True if the dates are both valid, else false.</returns>
        private bool IsValid()
        {
            if (StartDate > EndDate)
            {
                MessageBox.Show("Your Start Date must be older than your End date!");
                return false;
            }

            if (EndDate > DateTime.Now)
            {
                MessageBox.Show("Your End Date cannot be for a date in the future.");
                return false;
            }

            if (StartDate < DateTime.Now.AddYears(-1))
            {
                MessageBox.Show("Your Start Date cannot be older than one year from today.");
                return false;
            }

            return true;
        }
    }
}
