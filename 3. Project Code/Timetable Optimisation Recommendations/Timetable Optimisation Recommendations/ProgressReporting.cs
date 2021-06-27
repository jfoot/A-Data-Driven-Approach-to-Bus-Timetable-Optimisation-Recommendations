// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.ComponentModel;
using System.Windows;

namespace Timetable_Optimisation_Recommendations
{
    /// <summary>
    /// Used to report back the progress of th task to the GUI.
    /// </summary>
    public class ProgressReporting : INotifyPropertyChanged
    {
        ///<value>Used to tell the GUI to refresh/update to the actual value.</value>
        public event PropertyChangedEventHandler? PropertyChanged;

        ///<value>The value of the task as a whole.</value>
        public double Value { get; set; }
        ///<value>The message of the whole task, saying everything that has been completed.</value>
        public string Message { get; set; }

        ///<value>Defines if a GUI element should be visible or not. Only shows the thing while progress is not completed.</value>
        public Visibility Visibility
        { 
            get
            {
                return (int)Value == 100 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// The default constructor, sets everything to be equal to zero.
        /// </summary>
        public ProgressReporting()
        {
            Message = "";
            Value = 0;
        }

        /// <summary>
        /// A constructor used to specify a tasks progress and a message for the task.
        /// </summary>
        /// <param name="value">The overall progress for the task.</param>
        /// <param name="message">The message of the last completed task.</param>
        public ProgressReporting(double value, string message)
        {
            Value = value;
            Message = message;
        }

        /// <summary>
        /// Used to update a progress reporter with information from another one.
        /// </summary>
        /// <param name="reporter">The progress reporter to take in feedback from.</param>
        public void Update(ProgressReporting reporter)
        {
            Message = Message + Environment.NewLine + reporter.Message;
            Value = reporter.Value;
            OnPropertyChanged();
        }

        /// <summary>
        /// Clears all values back to zero/null.
        /// </summary>
        public void Clear()
        {
            Message = "";
            Value = 0;
            OnPropertyChanged();
        }


        /// <summary>
        /// Used to tell the GUI to update whenever a value in th class is changed.
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged(string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
