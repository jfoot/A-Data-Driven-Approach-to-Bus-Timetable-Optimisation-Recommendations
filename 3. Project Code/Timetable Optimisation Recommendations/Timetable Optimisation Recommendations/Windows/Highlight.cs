// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Windows.Media;

namespace Timetable_Optimisation_Recommendations.Windows
{
    /// <summary>
    /// Used to tell the timetable data grid what colour highlights the cells should be.
    /// </summary>
    public class Highlight
    {
        ///<value>The X coordinate in the grid.</value>
        public int X { get; init; }
        ///<value>The Y coordinate in the grid.</value>
        public int Y { get; init; }
        ///<value>Is it for the outbound or inbound table.</value>
        public bool IsOutbound { get; init; }
        ///<value>The total weighting cell colour.</value>
        public SolidColorBrush TotalWeight { get; init; } = Brushes.Green;
        ///<value>The slack time cell colour</value>
        public SolidColorBrush SlackWeight { get; init; } = Brushes.Green;
        ///<value>The cohesion value cell colour</value>
        public SolidColorBrush CohesionWeight { get; init; } = Brushes.Green;
        ///<value>If it's a moved record or not cell colour.</value>
        public SolidColorBrush MoveHighlight { get; set; } = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
    }
}
