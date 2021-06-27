// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Pages;
using Timetable_Optimisation_Recommendations.Timetable_Analyser;
using Timetable_Optimisation_Recommendations.Windows;

namespace Timetable_Optimisation_Recommendations.UserControls
{
    /// <summary>
    /// The card used to show a timetable cluster and the groupings within it.
    /// </summary>
    public partial class ClusterCard : UserControl
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ClusterCard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called upon when the user clicks on the timetable cluster to optimize for it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OptimiseClick(object sender, RoutedEventArgs e)
        {
            NavigationService ns = NavigationService.GetNavigationService(this)!;

            if (DataContext is Cluster cluster)
                ns.Navigate(new RouteSegmentSelector(cluster));
        }

        /// <summary>
        /// Called upon when the user asks to view the timetable for that cluster.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewTimetableClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is Cluster cluster)
            {
                ViewTimetable viewer = new(cluster.BusTimeTables, cluster.GetAssociatedService()) { Owner = Window.GetWindow(this) };
                viewer.Show();
            }
        }
    }
}
