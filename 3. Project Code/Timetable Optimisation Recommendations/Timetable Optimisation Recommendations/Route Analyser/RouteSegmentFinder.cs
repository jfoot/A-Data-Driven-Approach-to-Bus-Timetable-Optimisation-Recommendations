// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Route_Analyser
{
    /// <summary>
    /// The route segment finder class takes in a Bus Service (known as the primary service)
    /// and a tolerance. It then finds any service which shares a common-route segment 
    /// with it, to the specified minimum segment length tolerance. 
    /// 
    /// The primary purpose of this is to find the shared bus-corridors. 
    /// </summary>
    public class RouteSegmentFinder
    {
        /// <value>
        /// The primary service for which you want to find services that share a route segment with.
        /// </value>
        public IBusService PrimaryService { get; }

        /// <value>
        /// All services that share a route segment with the primary services and the stops in the segment. 
        /// </value>
        private readonly List<RouteSegment> _linkedServices =  new();

        /// <value>
        /// The minimum length of a segment to be counted as shared. 
        /// This should be at least 2. Otherwise you just get any service 
        /// that is at any of the same stop.
        /// </value>
        private readonly int _routeSegmentMinimum;

        /// <value>
        /// Used to store if the object has been Initialise yet or not.
        /// </value>
        private bool _isInitialised = false;



        /// <summary>
        /// Default constructor for the route segment finder.
        /// </summary>
        /// <param name="primaryService">The service you want to find common route segments with</param>
        /// <param name="routeSegmentMinimum">the minimum length of a route segment to count, only used if you wish to override settings variable.</param>
        public RouteSegmentFinder(IBusService primaryService, int? routeSegmentMinimum = null)
        {
            PrimaryService = primaryService;
            _routeSegmentMinimum = routeSegmentMinimum ?? Properties.Settings.Default.SharedRouteSegMin;
        }


    
        /// <summary>
        /// Finds any services which might have a shared route segment and what the segment contains.
        /// </summary>
        /// <returns>A list of found route segments.</returns>
        public async Task<List<RouteSegment>> FindSharedRouteSegmentsAsync(IProgress<ProgressReporting>? progress)
        {
            //If this is the first time calling the function do work.
            if (!_isInitialised)
            {
                _isInitialised = true;
                //Used as longer-term memory to keep track of all solutions found.
                //There is a time where this will be empty and short term memory has the answers.
                List<RouteSegment> tempSegments = new();

                //For each bus stop in the primary service
                IBusStop[] array = await PrimaryService.GetLocations(Direction.Both);
                for (int i = 0; i < array.Length; i++)
                {
                    IBusStop stop = array[i];
                    //Used to store continued or newly identified segments at a current stop.
                    List<RouteSegment> shortTermSegments = new();

                    //Get all the other services that are this stop.
                    foreach (IBusService service in stop.GetServices())
                    {
                        //If it is itself then skip.
                        if (WeakEquals(service, PrimaryService))
                            continue;
                    
                        //If this route segment has already been found, i.e one or more previous consecutive stops have had this service.
                        //Then also add this stop to that segment chain.
                        if (tempSegments.Any(tempSegment => WeakEquals(tempSegment.SecondaryService, service)))
                        {
                            //Finds the segment.
                            RouteSegment segment = tempSegments.Single(tempSegment => WeakEquals(tempSegment.SecondaryService, service));
                            //Adds this new stop to the segment chain.
                            segment.Stops.Add(stop);
                            //Removes from the temporary memory.
                            tempSegments.Remove(segment);
                            //Adds it to the short term memory.
                            shortTermSegments.Add(segment);
                        }
                        //Else this is the first time this route segment has been found.
                        else
                        {
                            shortTermSegments.Add(new RouteSegment(service, stop));
                        }
                    }

                    AddToLongTerm(tempSegments);

                    //Make the tempSegments equal to the short term Segments.
                    tempSegments = shortTermSegments;

                    progress?.Report(new ProgressReporting(i / (double)array.Length * 100.0, "Found Services at Stop : " + stop.CommonName));
                }

                AddToLongTerm(tempSegments);

     
                //Check that these solutions are correct.
                await Validator(progress);
            }

            progress?.Report(new ProgressReporting(100.0, "Completed Task"));
            //Return back the results.
            return _linkedServices;
        }


        /// <summary>
        /// This function is called inside the "findSharedRouteSegmentsAsync()" function to validate the answers found.
        /// The first issue that could occur is one service follows the exact route of another for a segment. But the same cannot be said for the other service.
        /// For example it might break off and re-join or stop at an extra stop elsewhere. I want to find two services that follow exactly the same set of stops.
        /// 
        /// The second issue is that the API can sometimes provide information about a service which doesn't fully exist in the API. Such as the 702.
        /// In these instances we need to remove them from the answer.
        /// </summary>
        /// <returns></returns>
        private async Task Validator(IProgress<ProgressReporting>? progress)
        {
            progress?.Report(new ProgressReporting(0.0, "Cross-validating Answers"));
            //For every solution found.
            for(int z = 0; z < _linkedServices.Count; z++)
            {
                try
                {
                    //Gets the stops that the secondary services visits.
                    IBusStop[] secondaryStops = await _linkedServices[z].SecondaryService.GetLocations(Direction.Both);
                    bool foundFirst = false;
                    int x = 0;
                    //Go through each of the stops in the secondary service.
                    for (int i = 0; i < secondaryStops.Length - 1; i++)
                    {
                        //If this stop matches the stop in the primary service segment enter.
                        if (WeakEquals(_linkedServices[z].Stops.ElementAt(x), secondaryStops[i]))
                        {
                            //State the first stop has been found.
                            foundFirst = true;
                            //Increment X so your looking for the next stop in the indefinite primary segment. 
                            //If we have reached the end of the segment then break free of the loop and stop the search.
                            if (++x == _linkedServices[z].Stops.Count - 1)
                                break;
                        }
                        //Else we have not found a matching segment.
                        //Were we half way through the discovery of a segment, (i.e. has at least the first been found), if so...
                        else if (foundFirst)
                        {
                            //If the segment before was long enough to be it's own segment then split the segment into two.
                            if (x > _routeSegmentMinimum)
                            {
                                //Create a new segment.
                                _linkedServices.Add(new RouteSegment(_linkedServices[z].SecondaryService, _linkedServices[z].Stops.GetRange(0, x)));
                                //Trim the original segment. 
                                _linkedServices[z].Stops.RemoveRange(0, x + 1);
                            }
                            //Else the new segment is to short to be classified as a viable segment so remove it.
                            else
                            {
                                //Trim the original segment. 
                                _linkedServices[z].Stops.RemoveRange(0, x + 1);
                                //Check that the remaining segment is still valid with the removed.
                                if (!IsValidSegment(_linkedServices[z]))
                                {
                                    _linkedServices.RemoveAt(z);
                                    break;
                                }
                            }
                            //Reset back to zero to search for the remainder of the segment. 
                            x = 0;
                            foundFirst = false;
                        }
                    }
                    //If it was never found then there is likely to be a major problem in the API.
                    if (!foundFirst)
                    {
                        _linkedServices.RemoveAt(z);
                        Console.WriteLine("WARNING : SERVICE " + _linkedServices[z].SecondaryService.ServiceId + " COULD NEVER FIND SEGMENET IN COLLECTION");
                    }

                }
                // This is likely to be triggered by an error in the API.
                catch (ReadingBusesAPI.ErrorManagement.ReadingBusesApiException ex)
                {
                    Console.WriteLine("WARNING : SERVICE " + _linkedServices[z].SecondaryService.ServiceId + " COULD NOT FIND LOCATIONS TO VALIDIATE : " + ex.Message);
                    _linkedServices.RemoveAt(z);
                }
                progress?.Report(new ProgressReporting(z + 1 / (double)_linkedServices.Count * 100.00, "Successfully cross-validated service : " + _linkedServices[z].SecondaryService.ServiceId));
            }          
        }




        /// <summary>
        /// Takes in tempSegments and then decides to add them to the final answers.
        /// tempSegments contains any route-segments that no longer continue onwards.
        /// i.e. the service has diverged from the primary service.
        /// </summary>
        /// <param name="tempSegments">Any route segments that have stopped.</param>
        private void AddToLongTerm(List<RouteSegment> tempSegments)
        {
            //The remaining segments in tempSegments variable are ones which are no longer following the bus's route.
            foreach (RouteSegment segment in tempSegments.Where(IsValidSegment))
            {
                _linkedServices.Add(segment);
            }
        }

        /// <summary>
        /// Finds all the distinct services which shared a route segment with us.
        /// One secondary service might have multiple segments, if they diverged and re-join 
        /// or inbound and outbound.
        /// </summary>
        /// <returns>An array of distinct services that have a route segment with the primary service.</returns>
        /// <remarks>
        /// This is includes it self, the primary service.
        /// </remarks>
        public async Task<IBusService[]> GetServicesInSegments(IProgress<ProgressReporting>? progress)
        {
            List<IBusService> temp = new() { PrimaryService };
            //Goes through all found route segments.
            foreach(RouteSegment service in await FindSharedRouteSegmentsAsync(progress))
                //Has the service already been recorded, if not it's not distinct, else add.
                if (!temp.Exists(serv => WeakEquals(serv, service.SecondaryService)))
                    temp.Add(service.SecondaryService);
            
            return temp.ToArray();
        }


        /// <summary>
        /// Given two services objects, check do they represent the same service. 
        /// Even if they might not be the same object, they logically are, i.e. they have the 
        /// same service id.
        /// </summary>
        /// <param name="service1">Service 1 to compare.</param>
        /// <param name="service2">Service 2 to compare.</param>
        /// <returns></returns>
        private static bool WeakEquals(IBusService service1, IBusService service2)
        {
            return service1.ServiceId == service2.ServiceId;
        }


        /// <summary>
        /// Given two Stop objects, check do they represent the same Stop. 
        /// Even if they might not be the same object, they logically are, i.e. they have the 
        /// same Stop id.
        /// </summary>
        /// <param name="stop1">Stop 1 to compare.</param>
        /// <param name="stop2">Stop 2 to compare.</param>
        /// <returns></returns>
        private static bool WeakEquals(IBusStop stop1, IBusStop stop2)
        {
            return stop1.AtcoCode == stop2.AtcoCode;
        }

        /// <summary>
        /// Works out if a segment is valid or not, which means is it long enough to be counted
        /// as a segment. 
        /// </summary>
        /// <param name="segment">The segment to check if it's valid.</param>
        /// <returns>Is the segment longer than the minimum value or not.</returns>
        private bool IsValidSegment(RouteSegment segment)
        {
            return segment.LengthOfSegment() >= _routeSegmentMinimum;
        }

    }
}
