// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search
{
    /// <summary>
    /// TabuList keeps track of what moves are tabu/invalid and what moves can now be performed.
    /// Moves are tabu while the tabu tenure is greater than zero.
    /// </summary>
    public class TabuList
    {

        /// <value>
        /// Used to keep track of what moves are currently tabu or not
        /// The key is the id of the move, and the int is the number of iterations remaining until no longer tabu.
        /// </value>
        private Dictionary<string, int> TabuListData { get; } = new();

        ///<value>The tabu tenure value, (how many iterations a move remains tabu)</value>
        private readonly int _tabuTenure;

        /// <summary>
        /// An optional constructor used to manually specify the TabuTenure,
        /// only use if you want to override the settings value.
        /// </summary>
        /// <param name="tabuTenure"></param>
        public TabuList(int? tabuTenure = null)
        {
            _tabuTenure = tabuTenure ?? Properties.Settings.Default.TabuTenure;
        }

        /// <summary>
        /// Once a move has been accepted it needs to be made tabu, and previous tabu records updated.
        /// </summary>
        /// <param name="move">The new move just been performed.</param>
        public void SetTabu(Move move)
        {
            //Update all other tabu-records now a new move has been made.
            //By deprecating their counter.
            foreach (string key in TabuListData.Keys)
            {
                //Decrement the tabu value by one.
                TabuListData[key] = TabuListData[key] - 1;

                //If tabu period has expired remove it.
                if (TabuListData[key] <= 0)
                    TabuListData.Remove(key);
            }


            //Add the new move into the tabu list.
            foreach (string changeId in move.ChangedRecordsIDs)
            {
                //Check that it's not already tabu, this should never happen.
                if (TabuListData.ContainsKey(changeId))
                    throw new Exception("This record is already Tabu, and is trying to be reset as Tabu.");

                //Add to the tabu-list.
                TabuListData.Add(changeId, _tabuTenure);
            }
        }

        /// <summary>
        /// Returns true if the move is going to be editing a tabu-timetable record or not.
        /// Therefore, false is an allowed move.
        /// </summary>
        /// <param name="move">The move to evaluate if it's tabu or not.</param>
        /// <returns>True or False for if it's tabu or not.</returns>
        public bool IsTabu(Move move)
        {
            return move.ChangedRecordsIDs.Any(changeId => TabuListData.ContainsKey(changeId));
        }

        /// <summary>
        /// Returns true if the record is considered tabu or not.
        /// </summary>
        /// <param name="record">A timetable record.</param>
        /// <returns>True if this record is tabu.</returns>
        public bool IsTabu(IBusTimeTable record)
        {
            return TabuListData.ContainsKey(record.GetId());
        }

        /// <summary>
        /// Used if all of the search space has become tabu, this should only happen if the user has set a really long
        /// tabu tenure or if they have a really small search space. It will go through and decrement all keys by one in the hopes
        /// that some are removed.
        /// </summary>
        public void FreeUpTabuListEarly()
        {
            //Update all other tabu-records now a new move has been made.
            //By deprecating their counter.
            foreach (string key in TabuListData.Keys)
            {
                //Decrement the tabu value by one.
                TabuListData[key] = TabuListData[key] - 1;

                //If tabu period has expired remove it.
                if (TabuListData[key] <= 0)
                {
                    TabuListData.Remove(key);
                    return;
                }
            }
        }
    }
}
