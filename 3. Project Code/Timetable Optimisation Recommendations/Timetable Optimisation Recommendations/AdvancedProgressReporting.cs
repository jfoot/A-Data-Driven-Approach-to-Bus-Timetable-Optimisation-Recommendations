// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

namespace Timetable_Optimisation_Recommendations
{
    /// <summary>
    /// Advanced Progress Reporting, is used to report back to the GUI on the progress of a more complex task
    /// that contains sub-tasks.
    /// </summary>
    public class AdvancedProgressReporting : ProgressReporting
    {
        ///<value>The progress of the sub value of the task.</value>
        public double SubValue { get; set; }

        ///<value>The Stage name of the task is currently on (if any)</value>
        public string? Stage { get; set; }
        ///<value>The Stage integer value it is on. Total number of stages isn't specified but normally three.</value>
        public int StageVal { get; set; }

        /// <summary>
        /// The default constructor for the advanced reporter.
        /// </summary>
        /// <param name="value">The value of the overall task.</param>
        /// <param name="subValue">The value of the sub-task.</param>
        /// <param name="message">A message to say what was last completed.</param>
        public AdvancedProgressReporting(double value, double subValue, string message) : base(value, message)
        {
            SubValue = subValue;
        }

        

        /// <summary>
        /// A constructor used to update the current stage message and value.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="stageVal"></param>
        public AdvancedProgressReporting(string stage, int stageVal) : this(0,0, "Starting New Stage - " + stage)
        {
            Stage = stage;
            StageVal = stageVal;
        }

        /// <summary>
        /// The default constructor for the class. 
        /// </summary>
        public AdvancedProgressReporting()
        {
        }

        /// <summary>
        /// Used to update an advanced progress reporter with another object.
        /// </summary>
        /// <param name="reporter"></param>
        public void Update(AdvancedProgressReporting reporter)
        {
            SubValue = reporter.SubValue;

            //Only changes the stage value if there is something to change it too, otherwise simply ignore it.
            if (reporter.Stage != null)
            {
                Stage = reporter.Stage;
                StageVal = reporter.StageVal;
            }
            
            base.Update(reporter);
        }


        /// <summary>
        /// Used to reset the progress back down to nothing/zero.
        /// </summary>
        public new void Clear()
        {
            SubValue = 0;
            base.Clear();
        }

    }
}
