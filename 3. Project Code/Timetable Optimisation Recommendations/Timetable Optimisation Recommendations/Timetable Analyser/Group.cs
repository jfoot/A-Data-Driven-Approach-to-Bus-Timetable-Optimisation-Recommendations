// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.UserControls;


namespace Timetable_Optimisation_Recommendations.Timetable_Analyser
{
    /// <summary>
    /// A group is a collection of consecutive days within a cluster. 
    /// </summary>
    public class Group
    {

        ///<value>A list of date spans, which contains the groups where times were the same.</value>
        public List<DateSpan>? Grouping { get; private set; }

        ///<value>Used by the GUI to know the groups and how/when to update.</value>
        public NotifyTaskCompletion<string> List { get; }

        ///<value>Contains the GUI grouping representation.</value>
        private string? _list;

        /// <summary>
        /// The default constructor for the group.
        /// </summary>
        /// <param name="associatedTimes">Takes in just a list of date times to group.</param>
        public Group(List<DateTime> associatedTimes)
        {
            //Start grouping the results.
            List = new NotifyTaskCompletion<string>(GetStringAsync(associatedTimes));
        }


        /// <summary>
        /// Generates a grouping within the cluster. A group is a set of consecutive days which share the same timetable. 
        /// This finds all the groups within the cluster and adds it to a list. A group can be one single day.
        /// </summary>
        /// <returns>Groupings within a cluster.</returns>
        public List<DateSpan> GroupingsOfClusters(List<DateTime> associatedTimes)
        {
            //If no dates is associated with the timetable there cannot be any groups.
            if (associatedTimes.Count == 0)
                return new List<DateSpan>();

           
            //Removes any duplicate data.
            associatedTimes = associatedTimes.Distinct().ToList();
            //Orders all of the dates into order.
            associatedTimes.Sort();

            //Adds the very first date to the list as it's own group.
            Grouping = new List<DateSpan>
            {
                new(associatedTimes[0])
            };

            //Then goes through all of the dates in the cluster and check, is this the day after the last identified group
            //in which case add it to the group. Else make a new grouping.
            DateTime lastDate = associatedTimes[0];
            for (int i = 1; i < associatedTimes.Count; i++)
            {
                DateTime currentDate = associatedTimes[i];
                //Is the day after the last group so add it.
                if ((currentDate - lastDate).TotalDays != 1)
                    Grouping.Add(new DateSpan(currentDate));
                //Is not, so generate a new group.
                else
                    Grouping.Last().AddDate(currentDate);

                //Records what the date of the last group was, this is the same as Grouping.Last().End
                lastDate = currentDate;
            }

            //Returns back the groupings found.
            return Grouping;
        }

        /// <summary>
        /// Generates the string for the GUI to output.
        /// </summary>
        /// <param name="associatedTimes">A list of date times to group.</param>
        /// <returns>A string summarizing the groups that have been found.</returns>
        public async Task<string> GetStringAsync(List<DateTime> associatedTimes)
        {
            //If this is the first time being called.
            if (_list == null)
            {
                await Task.Run(() =>
                {
                    //Find the grouping and then go through it building up the string. 
                    foreach (DateSpan? group in GroupingsOfClusters(associatedTimes))
                    {
                        _list += " • " + group.ToString() + Environment.NewLine;
                    }
                });
            }
          
            return _list ?? string.Empty;
        }

      



        /// <summary>
        /// A class used to represent a date span, which is a period between two dates.
        /// </summary>
        public class DateSpan
        {
            /// <summary>
            /// The start date of the span, this should be the oldest of the dates.
            /// </summary>
            public DateTime Start { get; private set; }
            /// <summary>
            /// The end date of the span, this should the newest of the dates.
            /// </summary>
            public DateTime End { get; private set; }


            /// <summary>
            /// A default constructor, which takes in an initial start and end date for the date span.
            /// </summary>
            /// <param name="start">An starting value for the date span.</param>
            /// <param name="end">An ending value for the date span.</param>
            public DateSpan(DateTime start, DateTime end)
            {
                Start = start.Date;
                End = end.Date;
                if (Start > End)
                    throw new Exception("Start date is not less than end date");
            }

            /// <summary>
            /// As the start and end initial values are likely to be the same.
            /// Until new data can be found, this constructor sets both to be the same.
            /// </summary>
            /// <param name="start">The start and end date for the date span</param>
            public DateSpan(DateTime start) : this(start, start)
            {
            }


            /// <summary>
            /// How long the span is, 0 for one day.
            /// </summary>
            /// <returns>How long the date span is.</returns>
            public int TotalSpan()
            {
                return (int)(End - Start).TotalDays;
            }


            /// <summary>
            /// Adds a new date to the date span, this date must be one newer or one day older than the current span.
            /// A span must be consecutive, so you cannot adjust the start or end span several days off the previous known cluster.
            /// </summary>
            /// <param name="date">A new start or end date for the span.</param>
            public void AddDate(DateTime date)
            {
                if (Start.Date.AddDays(-1) == date.Date)
                {
                    IncreaseStart();
                }
                else if (End.Date.AddDays(1) == date.Date)
                {
                    IncreaseEnd();
                }
                else
                {
                    throw new Exception("Data can not be appended to the cluster");
                }
            }

            /// <summary>
            /// Increases the end day by one.
            /// </summary>
            private void IncreaseEnd()
            {
                End = End.Date.AddDays(1);
            }

            /// <summary>
            /// Increases the start day by one.
            /// </summary>
            private void IncreaseStart()
            {
                Start = Start.Date.AddDays(-1);
            }


            /// <summary>
            /// Returns if the date-span is only contained within a weekday (Monday to Friday)
            /// Or if it contains weekends as well. This is because I'm assuming weekdays
            /// will have generally contestant timetables.
            /// </summary>
            /// <returns></returns>
            public bool IsWeekDay()
            {
                return (Start.Date.DayOfWeek >= DayOfWeek.Monday && Start.Date.DayOfWeek <= DayOfWeek.Friday) &&
                    (End.Date.DayOfWeek >= DayOfWeek.Monday && End.Date.DayOfWeek <= DayOfWeek.Friday);
            }


            /// <summary>
            /// The string representation of the group.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Start.ToShortDateString() + " (" + Start.DayOfWeek + ") to " + End.ToShortDateString() + " (" + End.DayOfWeek + ")";
            }
        }
    }
}
