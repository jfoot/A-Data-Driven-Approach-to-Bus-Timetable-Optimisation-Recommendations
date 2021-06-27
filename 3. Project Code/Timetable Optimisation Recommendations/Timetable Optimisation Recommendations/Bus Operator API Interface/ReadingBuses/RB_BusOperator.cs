// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.ErrorManagement;
using ReadingBusesAPI.Common;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusOperator interface for the Reading Buses API.
    /// </summary>
    public sealed class RbBusOperator : IBusOperator
    {
        [JsonProperty] public ConcurrentDictionary<string, RbBusStop> Locations { get; private set; } = new();
        [JsonProperty] public ConcurrentDictionary<string, RbBusService> Services { get; private set; } = new();

        [JsonIgnore] private readonly Task<ReadingBusesAPI.ReadingBuses> _apiInstance;

        [JsonIgnore] private static RbBusOperator? _instance;

        [JsonIgnore] public static readonly string CacheDirectory = "cache/ReadingBuses";


        private RbBusOperator(string apiKey)
        {
            _apiInstance = Task.Run(() => ReadingBusesAPI.ReadingBuses.Initialise(apiKey));
        }




        /// <summary>
        ///     Used to initially initialise the ReadingBuses Object, it is recommended you do this in your programs start up.
        /// </summary>
        /// <param name="apiKey">The Reading Buses API Key, get your own from http://rtl2.ods-live.co.uk/cms/apiservice </param>
        /// <returns>An instance of the library controller. This same instance can be got by calling the "GetInstance" method.</returns>
        /// <exception cref="ReadingBusesApiExceptionBadQuery">Can throw an exception if you pass an invalid or expired API Key.</exception>
        /// See
        /// <see cref="Bus_Operator_API_Interface.ReadingBuses.GetInstance()" />
        /// to get any future instances afterwards.
        public static async Task<IBusOperator> Initialise(string apiKey)
        {
            if (_instance == null)
            {
                string fqnLocations = CacheDirectory + "/" + "ReadingBuses_Locations.json";
                string fqnService = CacheDirectory + "/" + "ReadingBuses_Services.json";

                _instance = new RbBusOperator(apiKey);


                if (File.Exists(fqnLocations) && File.Exists(fqnService))
                {
                    try
                    {
                        _instance.Locations = JsonConvert.DeserializeObject<ConcurrentDictionary<string, RbBusStop>>(File.ReadAllText(fqnLocations)) ?? new ConcurrentDictionary<string,RbBusStop>();
                        _instance.Services = JsonConvert.DeserializeObject<ConcurrentDictionary<string, RbBusService>>(File.ReadAllText(fqnService)) ?? new ConcurrentDictionary<string, RbBusService>();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning : Cache Read Failed - " + ex.Message);
                        File.Delete(fqnService);
                        File.Delete(fqnLocations);
                        return await Initialise(apiKey);
                    }
                }
                else
                {
                    ReadingBusesAPI.ReadingBuses rbInstance = await _instance._apiInstance;

                    foreach (RbBusStop stop in Array.ConvertAll(rbInstance.GetLocations(), item => (RbBusStop)item))
                        _instance.Locations.TryAdd(stop.AtcoCode, stop);

                    foreach (RbBusService service in Array.ConvertAll(rbInstance.GetServices(Company.ReadingBuses), item => (RbBusService)item))
                        _instance.Services.TryAdd(service.ServiceId, service);

                    _instance.ForceUpdateCache();
                }
            }

            return _instance;
        }



        public bool IsService(string serviceNumber) => Services.ContainsKey(serviceNumber);


        public IBusStop GetLocation(string atcoCode)
        {
            if (Locations.TryGetValue(atcoCode, out RbBusStop? stop))
                return stop;
            
            throw new Exception("A bus stop of that Atco Code can not be found, please make sure you have a valid Bus Stop Code. You can use, the 'IsLocation' function to check beforehand.");
        }



        /// <summary>
        ///     Checks to see if the acto code for the bus stop exists in the API feed or not.
        /// </summary>
        /// <param name="atcoCode">The ID Code for a bus stop.</param>
        /// <returns>True or False depending on if the stop is in the API feed or not.</returns>
        public bool IsLocation(string atcoCode) => Locations.ContainsKey(atcoCode);

        public void InvalidateCache()
        {
            Directory.Delete(CacheDirectory, true);
        }


        public static IBusOperator GetInstance()
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    "You must first initialise the object before usage, call the 'Initialise' function passing your API credentials.");
            }

            return _instance;
        }


        public IBusService GetService(string serviceNumber)
        {
            if (Services.TryGetValue(serviceNumber, out RbBusService? service))
                return service;
            
            throw new Exception("The service number provided does not exist. You can check if it exists by calling 'IsService' first.");           
        }



        public void ForceUpdateCache()
        {
            if (_instance != null)
            {
                CacheWriter.WriteToCache(CacheDirectory, "ReadingBuses_Locations.json", JsonConvert.SerializeObject(_instance.Locations, Formatting.Indented));
                CacheWriter.WriteToCache(CacheDirectory, "ReadingBuses_Services.json", JsonConvert.SerializeObject(_instance.Services, Formatting.Indented));
            }
        }

        public IBusService[] GetServices()
        {
            return Services.Values.OrderBy(item => Convert.ToInt32(Regex.Replace(item.ServiceId, "[^0-9.]", ""))).ToArray();
        }
    }
}
