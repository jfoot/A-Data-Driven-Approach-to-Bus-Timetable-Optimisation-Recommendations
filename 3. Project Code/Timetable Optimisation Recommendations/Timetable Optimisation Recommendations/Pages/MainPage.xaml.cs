// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Windows;

namespace Timetable_Optimisation_Recommendations.Pages
{
    
    /// <summary>
    /// The main start page for the program, this is where the user will select the primary
    /// service that they wish to optimize for.
    /// </summary>
    public partial class MainPage : Page
    {
        ///<value>Stores a list of bus services that the operator operates.</value>
        public ObservableCollection<IBusService> ServiceCardCollection { get; } = new(BusOperatorFactory.Instance.Operator.GetServices());

        /// <summary>
        /// The default program entry point constructor.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called upon if the user clicks on the settings button, launches up the settings page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LaunchSettings(object sender, RoutedEventArgs e)
        {
            Settings settingsPage = new()
            {
                Owner = Window.GetWindow(this)
            };
            settingsPage.Show();
        }
    }
}
