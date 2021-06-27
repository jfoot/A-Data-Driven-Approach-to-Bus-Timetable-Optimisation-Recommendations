// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Timetable_Evaluator
{
    /// <summary>
    /// A "blamed" bus timetable is a timetable record that also contains weights and blame values, from squeaky wheel optimization.
    /// Along with the logic for updating the scheduled times.
    /// </summary>
    public class BlamedBusTimeTable : IBusTimeTable, ICloneable
    {
        ///<value>Stores the blame values for the slack time objective.</value>
        public Weights SlackWeights { get; private set; } = new ();
        ///<value>Stores the blame values for the cohesion objective.</value>
        public Weights CohesionWeights { get; private set; } = new ();

        ///<value>Stores the total blame for the record as a whole.</value>
        public double TotalWeight { get; private set; } = 0;

        ///<value>Stores the Scheduled Arrival Time</value>
        public DateTime SchArrivalTime { get; private set; }

        ///<value>Stores the Scheduled Departure Time</value>
        public DateTime SchDepartureTime { get; private set; }
        
        ///<value>A reference is kept to the original record as lazy evaluation means it would be impractical to deep clone.</value>
        private readonly IBusTimeTable _record;

        /// <summary>
        /// The default constructor for the blamed record timetable, takes in a pre-existing timetable record.
        /// </summary>
        /// <param name="record">The pre-existing timetable record.</param>
        public BlamedBusTimeTable(IBusTimeTable record)
        {
            _record = record;
            SchArrivalTime = _record.SchArrivalTime;
            SchDepartureTime = _record.SchDepartureTime;
        }


        


        /// <summary>
        /// Updates the Scheduled Arrival and Departure times to their suggested values calculated from their blame values in SWO.
        /// This should only be called on the target record.
        /// </summary>
        public void SetSuggestedToReal()
        {
            //Generates a new scheduled arrival time weighted between the suggestions of the optimisation parameters.
            SchArrivalTime = ProposedSchArrivalTime();
            //Generates a new scheduled departure time weighted between the suggestions of the optimisation parameters
            SchDepartureTime = ProposedSchDepartureTime();

            //Resets weights for the record as the scheduled arrival and departure times has now changed.
            ResetWeights();
        }

        /// <summary>
        /// Calculates the proposed scheduled arrival time.
        /// Uses the blame values and weights to suggest a new time.
        /// </summary>
        /// <returns>The best new arrival time for this record</returns>
        public DateTime ProposedSchArrivalTime()
        {
            return SlackWeights.TargetSchArrivalTime;

            //This is the code that would use both objective functions.
/*
            double totalDominance = SlackTimeEvaluator.Dominance + ServiceCohesionEvaluator.Dominance;
            //Generates a new scheduled arrival time weighted between the suggestions of the optimisation parameters.
            return new DateTime(Convert.ToInt64((SlackWeights.TargetSchArrivalTime.Ticks * SlackTimeEvaluator.Dominance / totalDominance)
                                                          + (CohesionWeights.TargetSchArrivalTime.Ticks * ServiceCohesionEvaluator.Dominance / totalDominance)));
*/
        }

        /// <summary>
        /// Calculates the proposed scheduled departure time.
        /// Uses the blame values and weights to suggest a new time.
        /// </summary>
        /// <returns>The best new departure time for this record.</returns>
        public DateTime ProposedSchDepartureTime()
        {
            return SlackWeights.TargetSchDepartureTime;
            
            //This is the code that would use both objective functions.
/*
            double totalDominance = SlackTimeEvaluator.Dominance + ServiceCohesionEvaluator.Dominance;
            //Generates a new scheduled departure time weighted between the suggestions of the optimisation parameters
            return new DateTime(Convert.ToInt64((SlackWeights.TargetSchDepartureTime.Ticks * SlackTimeEvaluator.Dominance / totalDominance)
                                                            + (CohesionWeights.TargetSchDepartureTime.Ticks * ServiceCohesionEvaluator.Dominance / totalDominance)));
*/
        }

        /// <summary>
        /// Performs a deep clone of the object.
        /// </summary>
        /// <returns>A deep clone of the current object,</returns>
        public object Clone()
        {
            BlamedBusTimeTable copy = (BlamedBusTimeTable)MemberwiseClone();
            copy.CohesionWeights = (Weights)CohesionWeights.Clone();
            copy.SlackWeights = (Weights)SlackWeights.Clone();
            
            return copy;
        }

        /// <summary>
        /// Given a new arrival and departure date update it accordingly within the record.
        /// </summary>
        /// <param name="arrival">new arrival time.</param>
        /// <param name="departure">new departure time.</param>
        public void UpdateTimes(DateTime arrival, DateTime departure)
        {
            SchArrivalTime = arrival;
            SchDepartureTime = departure;
            ResetWeights();
        }

        /// <summary>
        /// Sets the weights back to their default values, this needs to happen whenever you change the arrival or departure times of a record,
        /// as when the times change the weights/ blame values also change with it.
        /// </summary>
        private void ResetWeights()
        {
            SlackWeights = new Weights();
            CohesionWeights = new Weights();
        }

        /// <summary>
        /// Generates the total weight, we do not need to apply dominance alterations here as we have
        /// already applied the dominance earlier on.
        ///
        /// This is the normalized weights added together. 
        /// </summary>
        public void UpdateTotalWeight()
        {
            TotalWeight = SlackWeights.Weight ?? 0;
            //TotalWeight = (SlackWeights.Weight ?? 0) + (CohesionWeights.Weight ?? 0);
        }

       
        /// <summary>
        /// The rest of the below is your standard code for a bus timetable record.
        /// </summary>

     
        public IBusStop Location => _record.Location;

        public long Sequence => _record.Sequence;

        public bool IsOutbound => _record.IsOutbound;

        public string JourneyCode => _record.JourneyCode;

        public string RunningBoard => _record.RunningBoard;

        public bool IsTimingPoint => _record.IsTimingPoint;

        public IBusService Service => _record.Service;

        public bool MatchDirection(Direction direction) => _record.MatchDirection(direction);

        public bool WeakIsStopSame(IBusTimeTable stop2)
        {
            return _record.WeakIsStopSame(stop2);
        }

        public bool WeakIsStopSame(IBusStop stop2)
        {
            return _record.WeakIsStopSame(stop2);
        }



#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override bool Equals(object obj)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        {
            return Equals(obj as BlamedBusTimeTable);
        }

        public bool Equals(BlamedBusTimeTable? other)
        {
            return other != null && SchArrivalTime == other.SchArrivalTime && SchDepartureTime == other.SchDepartureTime && JourneyCode == other.JourneyCode && RunningBoard == other.RunningBoard && Sequence == other.Sequence && Location.AtcoCode == other.Location.AtcoCode;
        }


        public override int GetHashCode() { return base.GetHashCode(); }


        public string GetId()
        {
            return _record.GetId();
        }


    }
}
