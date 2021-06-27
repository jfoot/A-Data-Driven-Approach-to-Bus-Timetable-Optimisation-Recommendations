// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Timetable_Optimisation_Recommendations.Windows
{
    /// <summary>
    /// The main settings page for the application.
    /// </summary>
    public partial class Settings : Window
    {
        /// <summary>
        /// The default constructor, sets up the GUI.
        /// </summary>
        public Settings()
        {
            InitializeComponent();
            SetGuiValues();
        }

        /// <summary>
        /// Given the current values of the settings, update the GUI to reflect them accordingly.
        /// </summary>
        private void SetGuiValues()
        {
            ApiKey.Text = Properties.Settings.Default.API_KEY;
            SegmentMinLength.Text = Properties.Settings.Default.SharedRouteSegMin.ToString();
            SlackDominance.Value = Properties.Settings.Default.SlackTimeDominance;
            CohesionDominance.Value = Properties.Settings.Default.CohesionDominance;
            IterationLimit.Text = Properties.Settings.Default.IterationLimit.ToString();
            TabuTenure.Text = Properties.Settings.Default.TabuTenure.ToString();
            CanadiateList.Text = Properties.Settings.Default.CandidateListSize.ToString();
            NeighborhoodSize.Text = Properties.Settings.Default.NeighborhoodSize.ToString();

            if (Properties.Settings.Default.WeakStop)
                TrueWeak.IsChecked = true;
            else
                FalseWeak.IsChecked = true;
        }

        /// <summary>
        /// Given what the user has typed/ selected update the settings accordingly.
        /// </summary>
        /// <returns>True if the settings are valid and accepted/changed, else false.</returns>
        private bool SetSettingsValues()
        {
            //Checks that the values the user has given are valid settings.
            if (ValidateShareRouteSeg() && ValidateCandidateNeighbourhood())
            {
                try
                {
                    Properties.Settings.Default.API_KEY = ApiKey.Text;
                    Properties.Settings.Default.SharedRouteSegMin = Int32.Parse(SegmentMinLength.Text);
                    Properties.Settings.Default.SlackTimeDominance = SlackDominance.Value;
                    Properties.Settings.Default.CohesionDominance = CohesionDominance.Value;
                    Properties.Settings.Default.WeakStop = TrueWeak.IsChecked ?? true;
                    Properties.Settings.Default.IterationLimit = Int32.Parse(IterationLimit.Text);
                    Properties.Settings.Default.TabuTenure = Int32.Parse(TabuTenure.Text);
                    Properties.Settings.Default.NeighborhoodSize = Int32.Parse(NeighborhoodSize.Text);
                    Properties.Settings.Default.CandidateListSize = Int32.Parse(CanadiateList.Text);

                    Properties.Settings.Default.Save();
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to parse user input, please do not paste in text to numerical only fields.");
                }
                return true;
            }
           
            return false;
        }

        /// <summary>
        /// Checks that the Shared Route Segment value is valid.
        /// </summary>
        /// <returns>True if greater than or equal to two and a number.</returns>
        private bool ValidateShareRouteSeg()
        {
            if (Int32.TryParse(SegmentMinLength.Text, out int value))
            {
                return value >= 2;
            }

            MessageBox.Show(this, "Shared Route Segment must be an int value greater than or equal to two.");
            return false;
        }

        /// <summary>
        /// Checks that the candidate and neighborhood sizes are correct and valid.
        /// </summary>
        /// <returns>True if candidate is less than neighborhood and both are greater than one.</returns>
        private bool ValidateCandidateNeighbourhood()
        {
            if (Int32.TryParse(CanadiateList.Text, out int candidate) && Int32.TryParse(NeighborhoodSize.Text, out int neighborhood))
            {
                return candidate <= neighborhood && neighborhood >= 1;
            }

            MessageBox.Show(this, "Candidate List Size must be less than or equal to neighborhood. Both must be greater than or equal to one.");
            return false;
        }

        /// <summary>
        /// Attempts to save the settings and close the window. If invalid the user will be prompted. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveClick(object sender, RoutedEventArgs e)
        {
            if(SetSettingsValues())
                Close();
        }

        /// <summary>
        /// Changes all of the settings back to their defaualt values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            Close();
        }

        /// <summary>
        /// Checks if the text is numerical or not, only positive integer values allowed. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsTextNumerical(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

    }
}
