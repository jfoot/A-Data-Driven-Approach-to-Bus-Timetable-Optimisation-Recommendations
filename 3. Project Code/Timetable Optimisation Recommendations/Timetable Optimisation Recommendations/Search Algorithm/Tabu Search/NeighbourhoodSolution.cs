// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;
using Timetable_Optimisation_Recommendations.Timetable_Simulator;

namespace Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search
{
    /// <summary>
    /// Used to generate the neighbourhood of solutions for the search algorithm.
    /// </summary>
    public class NeighbourhoodSolution
    {
        ///<value>The neighborhood size.</value>
        private readonly int _neighbourhoodSize;
        ///<value>The candidate list size.</value>
        private readonly int _candidateListSize;

        ///The reference to the main evaluator object.
        private readonly TimeTableEvaluator _evaluator;


        /// <summary>
        /// The default constructor for the NeighbourhoodSolution class.
        /// </summary>
        /// <param name="evaluator">A reference to the main evaluator.</param>
        /// <param name="neighbourhoodSize">The size of the neighborhood to generate, only needed if not default, should be greater than 1, preferably bigger..</param>
        /// <param name="candidateListSize">The size of the candidate list to generate, only needed if not default, should be less than or equal to neighborhood.</param>
        public NeighbourhoodSolution(TimeTableEvaluator evaluator, int? neighbourhoodSize = null, int? candidateListSize = null)
        {
            _evaluator = evaluator;
            _neighbourhoodSize = neighbourhoodSize ?? Properties.Settings.Default.NeighborhoodSize;
            _candidateListSize = candidateListSize ?? Properties.Settings.Default.CandidateListSize;
        }


        /// <summary>
        /// Generates the negibourhood of solutions, returns an array of tuples of solutions, the solution only contains changes to one service,
        /// it is assumed that all other services results won't have changed. This is done for efficiency purposes, to both save memory and because you
        /// don't need to recalculate objective function value on everything.
        /// </summary>
        /// <returns>An array of solutions, one per candidate list.</returns>
        /// <remarks>
        /// It is theoretically possible, but very unlikely that this would return back zero moves.
        /// But it can do if a lot of the search space is tabu and there is only a small search space.
        /// Regardless this should be checked for.
        /// </remarks>
        public async Task<Move[]> GenerateNegibourhood(TabuList tabuList, IProgress<AdvancedProgressReporting>? progress = null)
        {
            //All of this code is used for the progress bar.
            int totalCompletedTasks = 0;
            int totalTasks = _candidateListSize;

            Progress<ProgressReporting> subProgress = new();
            subProgress.ProgressChanged += delegate (object? o, ProgressReporting d)
            {
                progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0 + (1.0 / totalTasks * d.Value),
                    d.Value, d.Message));
            };
            
            //Used to store the list of generated moves.
            List<Move> moves = new(_candidateListSize);

            progress?.Report(new AdvancedProgressReporting("Stage 2 : Generating Negibourhood", 2));

            progress?.Report(new AdvancedProgressReporting(0, 100, "Identifying high-blame areas and applied a probabilistic selection."));

            //ShallowClone (Should be okay though)
            List<BlamedBusTimeTable> flattenedBlame = _evaluator.CurrentSolution.BusTimeTables.Values.SelectMany(x => x).OrderBy(r => r.TotalWeight).ToList();
            
            do
            {
                //Find the timetable records that most problematic 
                List<BlamedBusTimeTable> targetAreas = await IdentifyHighBlameAreas(tabuList, flattenedBlame);

                //Go through the solution neighborhood and probabilistically select moves to make.
                //Checking if the move is tabu or not. If it is skip it and find another one.
                while (targetAreas.Count != 0 && moves.Count != _candidateListSize)
                {
                    //Takes out the move from the neighborhood probabilistically and then generate a new move for it.
                    Move temp = await GenerateMoveAsync(ProbabilisticSelection(targetAreas), subProgress);

                    //Then need to check if this move is tabu or not.
                    //If move is not tabu then accept it. Else do nothing and try again.
                    if (!tabuList.IsTabu(temp))
                    {
                        moves.Add(temp);
                        totalCompletedTasks = moves.Count;
                        progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0, 100, Environment.NewLine + "Identified a new move : " + Environment.NewLine + temp + Environment.NewLine));
                    }
                    else
                    {
                        progress?.Report(new AdvancedProgressReporting(totalCompletedTasks / (double)totalTasks * 100.0, 100, "MOVE WAS TABU - Finding new move"));
                        Console.WriteLine("Move was tabu, trying again!");
                    }
                }

                //If a valid move could not be found in the first candidate list, generate a new candidate list from the next X best.
                //Eg is the first 10 best moves are all tabu, look at the 20 best moves and so on, until you run out of moves to make.
                //This edge case is very unlikely to trigger, but just in case there is a very small search
                //space or a large tabu-tenure/list, where it is stuck in a local minima, perform this.
            } while (moves.Count < _candidateListSize && flattenedBlame.Count != 0);

  
            //Return an array of possible solutions.
            return moves.ToArray();
        }


        /// <summary>
        /// Identify all of the time table records that have the highest "blame", across all services and return this as a list.
        /// This should in-theory be the top X most problematic records.
        /// </summary>
        /// <returns>The top X blamed records.</returns>
        private async Task<List<BlamedBusTimeTable>> IdentifyHighBlameAreas(TabuList tabuList, List<BlamedBusTimeTable> flattenedBlame)
        {
            //Flatten the 2D array into a single array and order based upon the total weight value.
             List<BlamedBusTimeTable> topRegions = new();

            //While there are items left to search through, realistically this will always be true.
            while (flattenedBlame.Count != 0)
            {
                if (tabuList.IsTabu(flattenedBlame.Last()) || await IsFirstOrLastStopRecord(flattenedBlame.Last()))
                {
                    //Remove the tabu value from the list and try to find another one.
                    flattenedBlame.Remove(flattenedBlame.Last());
                }
                else
                {
                    //Add the highest blame value record to the top region list.
                    topRegions.Add(flattenedBlame.Last());
                    //Removes all other records that are in "same region".
                    flattenedBlame.RemoveAll(r => IsSameRegion(topRegions.Last(), r));
                    //If we have got enough areas identified break free and stop.
                    if (topRegions.Count == _neighbourhoodSize)
                        break;
                }
            }

            //Return the high risk areas.
            return topRegions;
        }


        /// <summary>
        /// Returns true if the timetable record is about the first or last stop of a service.
        /// These stops prove difficult to estimate slack time and dwell times so can not be directly selected as a move.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private static async Task<bool> IsFirstOrLastStopRecord(BlamedBusTimeTable record)
        {
            IBusStop[] inbound = await record.Service.GetLocations(Direction.Inbound);
            IBusStop[] outbound = await  record.Service.GetLocations(Direction.Outbound);

            return (inbound.Length != 0 && (record.Location == inbound.First() || record.Location == inbound.Last())) 
                   || (outbound.Length != 0 && (record.Location == outbound.First() || record.Location == outbound.Last()));
        }



        /// <summary>
        /// Given a single target area that is an issue, apply a fix to that area, then adjust the surrounding timetable records
        /// to make sure the changes are propagated, but use a drop-off function to return it back to the initial solution to
        /// prevent an overzealous propagation of changes.
        /// </summary>
        /// <param name="target">A single timetable record identified as an issue.</param>
        /// <param name="progress">Used to report back the progress of the function to the GUI or listener.</param>
        /// <returns>A tuple of the Bus Service the timetable records pertain too, along with an array of updated timetable records.</returns>
        private async Task<Move> GenerateMoveAsync(BlamedBusTimeTable target, IProgress<ProgressReporting>? progress = null)
        {
            //Creates a deep clone of the array and it's objects.
            //This is so that it doesn't effect the initial solution in the holding array.

            if (_evaluator.CurrentSolution.BusTimeTables.ContainsKey(target.Service))
            {
                Move move = new Move
                {
                    Service = target.Service,
                    TimeTable =
                        //Deep clone the timetable.
                        _evaluator.CurrentSolution.BusTimeTables[target.Service]
                            .Select(a => (BlamedBusTimeTable)a.Clone()).ToArray(),
                    ChangedRecordsIDs = new List<string> {target.GetId()},
                    ProposedSchArrivalTime = target.ProposedSchArrivalTime(),
                    ProposedSchDepartureTime = target.ProposedSchDepartureTime(),
                    TargetRecord = target,
                    ChangeAmount = (target.ProposedSchArrivalTime() - target.SchArrivalTime).TotalMinutes
                };


                //Finds only records that are in the same running board. As we don't ever propagate further than this 
                //and it ensures that they are sequential records, to be sure we also order the records.
                BlamedBusTimeTable[] associatedRanges =
                    move.TimeTable.Where(r => r.RunningBoard == target.RunningBoard).ToArray();
                //.OrderBy(r => r.JourneyCode).ThenBy(r => r.Sequence).ToArray();

                //Finds the index of the object in the new deep-copy array, this is important as the object reference has now changed!
                int position = Array.IndexOf(associatedRanges, target);


                //Checks it can be found, this should never fail, but just encase.
                if (position == -1)
                    throw new IndexOutOfRangeException("Target record not found running board sub-set.");


                //Step 1 - First find the regions before and after the target area for which propagation will be applied.

                //Generate a random drop off time between 20 to 45 min from the change location, this is to limit propagation from being to large.
                DateTime dropOffForwardsTime = target.SchArrivalTime.AddMinutes(new Random().Next(25, 45));
                //Selected a subset of records that are within the drop-off range.
                BlamedBusTimeTable[] subSetForwards = associatedRanges.Skip(position)
                    .TakeWhile(r => r.SchArrivalTime <= dropOffForwardsTime).ToArray();

                


                //Step 2 - Apply the propagation onto the values.

                //Sets the target record to have the suggested departure and arrival times.
                associatedRanges[position].SetSuggestedToReal();

                //Fix the timetable going forwards.
                await PropagateTimesForwardsAsync(subSetForwards, dropOffForwardsTime, progress);
                //Record the move as changing these records.
                AddChanges(move.ChangedRecordsIDs, subSetForwards);
                
                return move;
            }
            

            return new Move();
        }

        /// <summary>
        /// Adds distinct changes to the move changed records list. This is used
        /// because the target record might be added in several times.
        /// </summary>
        /// <param name="ids">The moves changed record IDs</param>
        /// <param name="records">An array of records that have been edited in some way.</param>
        private static void AddChanges(List<string> ids, BlamedBusTimeTable[] records)
        {
            foreach (BlamedBusTimeTable record in records)
                if(!ids.Contains(record.GetId()))
                    ids.Add(record.GetId());
        }


        



        /// <summary>
        /// Used to apply propagation throughout the rest of the timetable, once you've edited one record, the subsequent records
        /// need to be moved forwards and backwards accordingly, but to stop this from going on forever a drop-off is used to ease it
        /// back to the original schedule. This can be done because I make the assumption that only small changes are going to be made
        /// subsequent iterations of the search will also fix any problems if they've been introduced. 
        /// </summary>
        /// <param name="plannedTimetable">A selection of time table records all from the same service. They should be sequential and in order of stops visited.</param>
        /// <param name="dropOffTime">The time for when propagation should stop and it should return back to the original time table.</param>
        /// <param name="progress">Used to report back the progress of the task as this can be a slow process.</param>
        /// <returns>An array of stub timetable records, representing the theoretical minimum value.</returns>
        /// <remarks>
        ///     Index 0 of planned timetable will always be the thing that's been moved, only propagate from 1 onwards.
        /// </remarks>
        private async Task PropagateTimesForwardsAsync(BlamedBusTimeTable[] plannedTimetable, DateTime dropOffTime, IProgress<ProgressReporting>? progress = null)
        {
            //Step 1:
            //Generate the theoretical new times it would take to continue traveling from this point forwards.
            //This is to get an understanding of how long it would take to travel given changes in traffic conditions at different points of the day.
            //This is used to prevent compound propagation of an exponential curve, instead a linear curve is desired. 
            BusTimeTableStub[] propagated = ConvertBlameToStubs(plannedTimetable);
          
            //Start at one because zero is the modified target. 
            for (int i = 1; i < propagated.Length; i++)
            {
                //The last departure time of the service.
                TimeSpan lastDepartureTime = propagated[i-1].SchDepartureTime.TimeOfDay;
                //The last stop it visited.
                IBusStop lastStop = propagated[i - 1].Location;
                //The stop it is currently traveling to.
                IBusStop nextStop = propagated[i].Location;

                //Gets all the services that go between the two stops and have been chosen as of interest.
                IBusService[] servicesOfRelevance = _evaluator.Collection.GetServices(lastStop, nextStop);

                //You could theoretically get ALL services that go between these two stops, even if a user hasn't choose to select them for optimizing
                //I have made the decision to not bother with this, one because I would need to keep track of the difference between what a user has chosen and what exists and two if the user hasn't chosen it.
                //They most likely don't wont to wait the extra time. Doing all services would take ages. 
                JourneyTimeSimulator journey = new(lastDepartureTime, lastStop, nextStop, servicesOfRelevance, _evaluator.RelatedDates);

                //The estimated time to travel between the two stops.
                TimeSpan timeToTravel = await journey.ProduceEstimatedTravelTimes();

                //The arrival time, which is equal to the last departure time + the time to travel.
                TimeSpan arrivalTime = lastDepartureTime + timeToTravel;

                //The amount of time given to wait between arrival to departure. Slack time isn't altered here as this is only propagation.
                TimeSpan slackTime = propagated[i].SchDepartureTime - propagated[i].SchArrivalTime;

                progress?.Report(new ProgressReporting(i / (double)propagated.Length * 100.0, "Propagated Change Forwards, was " + propagated[i].SchArrivalTime.ToString("HH:mm") + " now, " + (propagated[i].SchArrivalTime.Date + arrivalTime).ToString("HH:mm")));
                
                //Actually sets the values of the stub records.
                propagated[i].SchArrivalTime = propagated[i].SchArrivalTime.Date + arrivalTime;
                propagated[i].SchDepartureTime = propagated[i].SchArrivalTime.AddMinutes(slackTime.TotalMinutes);
            }

            


            //Step 2:
            //Go through and weight all the values, so that it will gradually reduce the propagation back to the original timetable values. 
            //This should be pretty quick so don't bother reporting the progress.
            for (int i = 1; i < plannedTimetable.Length; i++)
            {
                //Propagation should make a single parabola, if it starts to go further than the drop-off
                //time then stop it from doing so, otherwise you have multiple peaks and troughs.
                if (propagated[i].SchArrivalTime >= dropOffTime)
                    break;

                //Weight the values between what they were originally scheduled to do to the theoretical new time. 
                WeightDropOff(ref plannedTimetable[i], plannedTimetable[0].SchArrivalTime, dropOffTime, propagated[i].SchArrivalTime);
            }


            progress?.Report(new ProgressReporting(100, "All Values Weighted Drop-Off Adjusted."));
        }

        /// <summary>
        /// Converts an Array of Blamed Bus Timetable records into Bus TimeTable Stubs.
        /// </summary>
        /// <param name="input">The input blamed array.</param>
        /// <returns>The associated time table stub array.</returns>
        private static BusTimeTableStub[] ConvertBlameToStubs(BlamedBusTimeTable[] input)
        {
            return Array.ConvertAll(input,r => new BusTimeTableStub(r));
        }


        /// <summary>
        /// The weighted drop off function is used to limit the amount of propagation a change can make in the timetable. If you change one record,
        /// then the subsequent record should also be adjusted and so on. This could lead to every record being effected if you alter the very first one.
        /// As we are making the assumption that a record is not going to be alter by a large amount we can "drop off" back to the original planned timetable.
        /// This is done using linear drop-off, where the two values are weighted based upon the closeness to the drop off point.
        /// </summary>
        /// <param name="record">A single timetable record</param>
        /// <param name="start">The time of the first record edited.</param>
        /// <param name="dropOffTime">The time of the drop-off point (last time allowed to be edited)</param>
        /// <param name="arrivalTime">The propagated drop-off time. Recommended by the journey time simulator</param>
        private static void WeightDropOff(ref BlamedBusTimeTable record, DateTime start, DateTime dropOffTime, DateTime arrivalTime)
        {
            //The amount of time given to wait between arrival to departure.
            TimeSpan slackTime = record.SchDepartureTime - record.SchArrivalTime;

            //Time between the propagated arrival time and the start time of the change.
            TimeSpan timeTillStart = arrivalTime - start;
            //Time between the planned arrival time and the drop off time.
            TimeSpan timeTillEnd = dropOffTime - arrivalTime;

            //The difference between the start of the changes and the drop off time. aka, the drop-off time/length
            TimeSpan totalSpan = dropOffTime - start;

            //The currently scheduled arrival time. 
            DateTime currentArrival = record.SchArrivalTime;

            //Generates a weighted average between the two time values, depending on which is closest (and re-adds on the date)
            //Start value is weighted with current and vice versa, as the bigger the gap the higher the weight, when it needs to be inverse.
            DateTime newArrivalTime = currentArrival.Date + (arrivalTime.TimeOfDay * (timeTillEnd / totalSpan) + currentArrival.TimeOfDay * (timeTillStart / totalSpan));

            //Set the new updated times to their weighted drop off times.
            record.UpdateTimes(newArrivalTime, newArrivalTime.AddMinutes(slackTime.TotalMinutes));
        }





        /// <summary>
        /// Implements Probabilistic Tabu-search, by randomly selecting a sub-sample of the neighborhood.
        /// This is done to encourage diversification and to further shrink the computational costs.
        /// </summary>
        /// <param name="topRegions">The full neighborhood.</param>
        /// <returns>A random sub-set of the full neighborhood, the candidate list.</returns>
        private static BlamedBusTimeTable ProbabilisticSelection(List<BlamedBusTimeTable> topRegions)
        {
            Random random = new();

            int tempIndex = random.Next(topRegions.Count);
            BlamedBusTimeTable record = topRegions[tempIndex];
            topRegions.RemoveAt(tempIndex);
           
            return record;
        }


        /// <summary>
        /// Checks if given to timetable records, they are about the same service and about the same journey code.
        /// This is because we don't want to get several highest blame functions from the same service and same
        /// journey code to encourage diversification. 
        /// </summary>
        /// <param name="record">Timetable record 1</param>
        /// <param name="record2">Timetable record 2</param>
        /// <returns>True if both records are about the same service and the same journey code, else false.</returns>
        private static bool IsSameRegion(IBusTimeTable record, IBusTimeTable record2)
        {
            return record.Service.IsWeakServiceSame(record2.Service) && record.JourneyCode == record2.JourneyCode;
        }

    }
}
