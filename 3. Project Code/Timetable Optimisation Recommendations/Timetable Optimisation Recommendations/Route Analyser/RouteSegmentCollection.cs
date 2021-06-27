// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Route_Analyser
{
    /// <summary>
    /// The RouteSegmentCollection class manages the results of the RouteSegmentFinder, and provides the logic for part
    /// of the GUI, which lets the user add or remove a service of interest. 
    /// </summary>
    public class RouteSegmentCollection
    {
        ///<value>The object that actually finds the route segments. </value>
        private RouteSegmentFinder? _finder;

        ///<value>
        /// A dictionary where the Key is a stop and the value is a list of services that are being monitored at the stop.
        /// This also includes all the stops of the primary service, with just it self in, regardless of if it could ever be
        /// a shared route segment. 
        /// </value>
        public Dictionary<IBusStop, List<IBusService>> ServicesAtStopOfInterest { get;  }= new();

        ///<value>Used by the GUI to store the included services.</value>
        public ObservableCollection<IBusService> IncludedServices { get;  } = new();
        ///<value>Used by the GUI to store the excluded services.</value>
        public ObservableCollection<IBusService> ExcludedServices { get; } = new();


        ///<value>Contains an array of all the services across all of the route segments.</value>
        private IBusService[]? _allServices;
        ///<value>All bus stops that area part of a shared route segment.</value>
        private IBusStop[]? _sharedStops;



        /// <summary>
        /// Used to actually initialise the object, given a route segment finder and a progress reporter.
        /// </summary>
        /// <param name="finderObj"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task InitialiseAsync(RouteSegmentFinder finderObj, IProgress<ProgressReporting>? progress = null)
        {
            //If it has been initialized before, then don't allow for it to be changed or re-initialized.
            if (_finder != null)
                return;

            _finder = finderObj; 

            //Initially just say that the primary service is at all of it's own stops.
            foreach (IBusStop? stop in await _finder.PrimaryService.GetLocations(Direction.Both))
                if(!ServicesAtStopOfInterest.ContainsKey(stop))
                    ServicesAtStopOfInterest.Add(stop, new List<IBusService> { _finder.PrimaryService });      

            //Then add in all other services from their route segments.
            await AddAllServices(progress);
        }


        /// <summary>
        /// Adds in all of the services as accepted as being apart of the route segment. 
        /// </summary>
        /// <param name="progress">A progress reporter for the GUI.</param>
        /// <returns></returns>
        private async Task AddAllServices(IProgress<ProgressReporting>? progress)
        {
            if (_finder is null)
                throw new NotSupportedException("You must first call 'InitialiseAsync' before using this object");
            
            foreach (RouteSegment? record in await _finder.FindSharedRouteSegmentsAsync(progress))
              foreach (IBusStop? stop in record.Stops)
                    if(ServicesAtStopOfInterest.ContainsKey(stop) && !ServicesAtStopOfInterest[stop].Contains(record.SecondaryService))
                        ServicesAtStopOfInterest[stop].Add(record.SecondaryService);

            IncludedServices.Clear();
            ExcludedServices.Clear();

            foreach (IBusService? service in await GetAllServicesAsync(progress))
                IncludedServices.Add(service);
        }

        /// <summary>
        /// Adds in a specific service to be included as part of the search.
        /// </summary>
        /// <param name="service">The service that you wish to add to the search space</param>
        /// <param name="progress">The progress of the task, shouldn't ever take very long.</param>
        /// <returns></returns>
        public async Task AddService(IBusService service, IProgress<ProgressReporting>? progress)
        {
            if (_finder is null)
                throw new NotSupportedException("You must first call 'InitialiseAsync' before using this object");

            if (service == _finder.PrimaryService)
                throw new NotSupportedException("Cannot add the primary service to a collection, already added by default.");

            if (IncludedServices.Any(ser => ser == service))
                return;

            //Goes through every segment that is about the service you are trying to add.
            foreach (RouteSegment? record in (await _finder.FindSharedRouteSegmentsAsync(progress)).Where(segment => segment.SecondaryService == service).ToArray())
                foreach (IBusStop? stop in record.Stops)
                    if (ServicesAtStopOfInterest.ContainsKey(stop))
                        if (!ServicesAtStopOfInterest[stop].Any(service => service == record.SecondaryService))
                            ServicesAtStopOfInterest[stop].Add(record.SecondaryService);

            //Adds to included and removes from excluded.
            IncludedServices.Add(service);
            ExcludedServices.Remove(service);
        }


        /// <summary>
        /// Removes a service from the search.
        /// </summary>
        /// <param name="service">The service that you wish to remove.</param>
        public void RemoveServiceAsync(IBusService service)
        {
            if (_finder is null)
                throw new NotSupportedException("You must first call 'InitialiseAsync' before using this object");

            if (service == _finder.PrimaryService)
                throw new Exception("Cannot remove the primary service from a collection");

            if (ExcludedServices.Any(ser => ser == service))
                return;

            //Go through and remove the service from everywhere in the dictionary.
            foreach (KeyValuePair<IBusStop, List<IBusService>> item in ServicesAtStopOfInterest)
                item.Value.Remove(service);

            IncludedServices.Remove(service);
            ExcludedServices.Add(service);
        }


        /// <summary>
        /// Given to bus stops, stop 1 and stop 2 find all the services that go between it.
        /// There is no guarantee that they do so consecutively 
        /// </summary>
        /// <param name="s1">Bus Stop 1</param>
        /// <param name="s2">Bus Stop 2</param>
        /// <returns></returns>
        public IBusService[] GetServices(IBusStop s1, IBusStop s2)
        {
            if (ServicesAtStopOfInterest.ContainsKey(s1) && ServicesAtStopOfInterest.ContainsKey(s2))
                return ServicesAtStopOfInterest[s1].Union(ServicesAtStopOfInterest[s2]).ToArray();

            return s1.GetServices().Union(s2.GetServices()).Where(ser => IncludedServices.Any(includedSer => includedSer == ser)).ToArray();
        }




        /// <summary>
        /// Gets an array of all the bus services that are apart of all route segments.
        /// </summary>
        /// <param name="progress">The progress reporter for this task.</param>
        /// <returns>An array of all services in the route segment.</returns>
        /// <remarks>
        /// This should only be used to check if you have all services cached and if you want to add all services.
        /// This does NOT give all services that are actually included. (it also includes excluded)
        /// </remarks>
        public async Task<IBusService[]> GetAllServicesAsync(IProgress<ProgressReporting>? progress)
        {
            if (_finder is null)
                throw new NotSupportedException("You must first call 'InitialiseAsync' before using this object");

            return _allServices ??= await _finder.GetServicesInSegments(progress);
        }



        /// <summary>
        /// Gets all stops that are apart of a shared route segment.
        /// </summary>
        /// <param name="progress">The progress reporter for the task.</param>
        /// <returns>An array of bus stops that are apart of a shared route segment.</returns>
        public async Task<IBusStop[]> GetAllSharedBusStopsAsync(IProgress<ProgressReporting>? progress)
        {
            if (_finder is null)
                throw new NotSupportedException("You must first call 'InitialiseAsync' before using this object");

            if (_sharedStops == null) {
                List<IBusStop> stops = new();

                foreach (RouteSegment? record in await _finder.FindSharedRouteSegmentsAsync(progress))
                    stops.AddRange(record.Stops);

                _sharedStops = stops.Distinct().ToArray();
            }

            return _sharedStops;
        }


        /// <summary>
        /// Gets an array of bus stops apart of the shared route-segment, including only stops that contains
        /// services that has been included. Unlike the GetAllSharedBusStopsAsync method above.
        /// </summary>
        /// <returns>All stops that are apart of a shared route segment.</returns>
        public IBusStop[] GetSharedBusStopsAsync()
        {
            List<IBusStop> stops = new();

            foreach ((IBusStop stop, List<IBusService> services)  in ServicesAtStopOfInterest)
            {
                if(services.Count >= 2)
                    stops.Add(stop);
            }

            return stops.ToArray();
        }
    }
}
