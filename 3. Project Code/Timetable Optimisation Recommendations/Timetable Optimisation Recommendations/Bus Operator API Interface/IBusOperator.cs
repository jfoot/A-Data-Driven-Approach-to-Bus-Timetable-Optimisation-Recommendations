// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// Provides all the information about a bus operator and gives the ability to query it further.
    /// </summary>
    public interface IBusOperator
    {

        /// <summary>
        ///     Returns a service which matches the Service Number passed.
        /// </summary>
        /// <param name="serviceNumber">The service number/ID for the service you wish to be returned eg: 17 or 22.</param>
        /// <returns>The services matching the ID.</returns>
        public IBusService GetService(string serviceNumber);



        /// <summary>
        ///     Checks to see if a service of that number exists or not in the API feed.
        /// </summary>
        /// <param name="serviceNumber">The service number to find.</param>
        /// <returns>True or False for if a service is the API feed or not.</returns>
        public bool IsService(string serviceNumber);


        /// <summary>
        /// Gets an array of all the IBusServices Objects.
        /// </summary>
        /// <returns>An array of all the bus services.</returns>
        public IBusService[] GetServices();


        /// <summary>
        ///     Get a bus stop location based upon a bus stops location code
        /// </summary>
        /// <param name="atcoCode">The code of the bus stop</param>
        /// <returns>A Bus Stop object for the Atco Code specified.</returns>
        public IBusStop GetLocation(string atcoCode);


        /// <summary>
        ///     Checks to see if the atco code for the bus stop exists in the API feed or not.
        /// </summary>
        /// <param name="atcoCode">The ID Code for a bus stop.</param>
        /// <returns>True or False depending on if the stop is in the API feed or not.</returns>
        public bool IsLocation(string atcoCode);


        /// <summary>
        /// Deletes any Cache data stored,  use this only if you need to force new data cache.
        /// </summary>
        public void InvalidateCache();


        /// <summary>
        /// Forces the current data stored in the bus operator object to be saved into Cache.
        /// You would need to do this if you've made some lazy API requests.
        /// </summary>
        public void ForceUpdateCache();
    }
}
