// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Timetable_Optimisation_Recommendations.Pages;

namespace Timetable_Optimisation_Recommendations.UserControls
{
    /// <summary>
    /// The service card, use to display the single service.
    /// </summary>
    public partial class ServiceCard : UserControl
    {
        public ServiceCard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called upon when the user decides that they want to optimise for this service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void  Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button) ?? throw new ArgumentException("Button was not sender");
            
            string serviceIdCalled = button.Tag.ToString()!;
            
            // Get a reference to the NavigationService that navigated to this Page
            NavigationService ns = NavigationService.GetNavigationService(this)!; 
            ns.Navigate(new DateSelector(serviceIdCalled));
        }
    }
}
