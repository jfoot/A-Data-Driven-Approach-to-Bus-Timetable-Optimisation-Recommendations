// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.BusServices;
using ReadingBusesAPI.Common;


namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusService interface for the Reading Buses API.
    /// </summary>
    public sealed class RbBusService : IBusService
    {
        [JsonProperty] public string ServiceId { get; private set; } = string.Empty;
        [JsonProperty] private string[]? _locationsAtcosOutBound;
        [JsonProperty] private string[]? _locationsAtcosInBound;

        [JsonIgnore] private IBusStop[]? _locationsOutBound;
        [JsonIgnore] private IBusStop[]? _locationsInBound;

        [JsonIgnore] private static readonly InternalCache<IBusHistoricTimeTable[]> ArchivedTimeTableCache = new();
        [JsonIgnore] private static readonly InternalCache<IBusTimeTable[]> TimeTableCache = new();




        public async Task<IBusStop[]> GetLocations(Direction direction = Direction.Both)
        {
            switch (direction)
            {
                //Get inbound data.
                case Direction.Inbound:
                    if (_locationsInBound == null)
                    {
                        if (_locationsAtcosInBound == null)
                        {
                            _locationsAtcosInBound =
                                await new BusService(ServiceId, Company.ReadingBuses).GetLocationsActo(false);
                            RbBusOperator.GetInstance().ForceUpdateCache();
                        }
                        _locationsInBound = GetLocations(_locationsAtcosInBound);
                    }
                    return _locationsInBound;
                //Get outbound data.
                case Direction.Outbound:
                    if (_locationsOutBound == null)
                    {
                        if (_locationsAtcosOutBound == null)
                        {
                            _locationsAtcosOutBound =
                                await new BusService(ServiceId, Company.ReadingBuses).GetLocationsActo(true);
                            RbBusOperator.GetInstance().ForceUpdateCache();
                        }
                        _locationsOutBound = GetLocations(_locationsAtcosOutBound);
                    }
                    return _locationsOutBound;

                //Get both inbound and outbound data.
                default:
                    //Get the inbound and outbound data separately.
                    List<IBusStop> inbound = (await GetLocations(Direction.Inbound)).ToList();
                    List<IBusStop> outbound = (await GetLocations(Direction.Outbound)).ToList();

                    //Add both together and return.
                    inbound.AddRange(outbound);
                    return inbound.ToArray();
            }
        }


        private static IBusStop[] GetLocations(string[]? locationAtco)
        {
            List<IBusStop> temp = new();
            foreach (string? location in locationAtco ?? Array.Empty<string>())
                if (RbBusOperator.GetInstance().IsLocation(location))
                    temp.Add(RbBusOperator.GetInstance().GetLocation(location));
            
            return temp.ToArray();
        }


        public bool IsArchivedTimeTableCached(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTable/" + ServiceId + "/Historic/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;

            return File.Exists(fqn);
        }

        public bool IsWeakServiceSame(IBusService service)
        {
            return service.ServiceId.Equals(ServiceId, StringComparison.OrdinalIgnoreCase);
        }


        public async Task<IBusHistoricTimeTable[]?> GetArchivedTimeTable(DateTime date)
        {
            return await ArchivedTimeTableCache.GetOrCreate(date.ToShortDateString() + "ARCHIVED" + ServiceId, async () => await FindArchivedTimeTable(date));
        }



        private async Task<IBusHistoricTimeTable[]?> FindArchivedTimeTable(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTable/" + ServiceId + "/Historic/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;


            if (File.Exists(fqn))
            {
                try
                {
                    RbTimeTableHistoric[]? temp = JsonConvert.DeserializeObject<RbTimeTableHistoric[]>(await File.ReadAllTextAsync(fqn));
                    return await FilterOutInvalidData(temp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Find Archived Bus Service Cache Timetable Failed : " + ex.Message);
                    File.Delete(fqn);
                    return await GetArchivedTimeTable(date).ConfigureAwait(false);
                }
            }
            else
            {
                try
                {
                    //This isn't thread safe, I've assumed that the singleton has already been created...
                    IBusHistoricTimeTable[] temp = Array.ConvertAll((await new BusService(ServiceId, Company.ReadingBuses).GetArchivedTimeTable(date)), item => (RbTimeTableHistoric)item);
                    temp = await FilterOutInvalidData(temp);
                    CacheWriter.WriteToCache(fileLoc, fileName, JsonConvert.SerializeObject(temp, Formatting.Indented));
                    return temp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to cache  " + ServiceId + " to cache : " + ex.Message);
                    //I write back to the cache null so that in future I do not make the same request again and keep getting null.
                    CacheWriter.WriteToCache(fileLoc, fileName, null);
                    return null;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private async Task<IBusHistoricTimeTable[]> FilterOutInvalidData(IBusHistoricTimeTable[]? records)
        {
            if (records is null)
                return Array.Empty<IBusHistoricTimeTable>();

            //The raw input data from the API for timetable records.
            List<IBusHistoricTimeTable> temp = records.ToList();
            //An array of locations on this services route.
            IBusStop[] locations = await GetLocations();

            //For each record, remove any that is about a stop not detailed in the service.
            for (int i = temp.Count - 1; i >= 0; i--)
                if (!locations.Any(loc => temp[i].WeakIsStopSame(loc)))
                    temp.RemoveAt(i);
            
            return temp.ToArray();
        }


        private async Task<IBusTimeTable[]?> FilterOutInvalidData(IBusTimeTable[]? records)
        {
            if (records is null)
                return records;

            //The raw input data from the API for timetable records.
            List<IBusTimeTable> temp = records.ToList();
            //An array of locations on this services route.
            IBusStop[] locations = await GetLocations();

            //For each record, remove any that is about a stop not detailed in the service.
            for (int i = temp.Count - 1; i >= 0; i--)
                if (!locations.Any(loc => temp[i].WeakIsStopSame(loc)))
                    temp.RemoveAt(i);

            return temp.ToArray();
        }



        public bool IsTimeTableCached(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTable/" + ServiceId + "/Planned/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;

            return File.Exists(fqn);
        }


        public async Task<IBusTimeTable[]?> GetTimeTable(DateTime date)
        {
            return await TimeTableCache.GetOrCreate(date.ToShortDateString() + "PLANNED" + ServiceId, createItem: async () => await FindTimeTable(date));
        }

        private async Task<IBusTimeTable[]?> FindTimeTable(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTable/" + ServiceId + "/Planned/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;


            if (File.Exists(fqn))
            {
                try
                {
                    RbTimeTable[]? temp = JsonConvert.DeserializeObject<RbTimeTable[]>(await File.ReadAllTextAsync(fqn));
                    return await FilterOutInvalidData(temp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Find Bus Service Cache Timetable Failed : " + ex.Message);
                    File.Delete(fqn);
                    return await GetTimeTable(date).ConfigureAwait(false);
                }
            }
            else
            {
                try
                {
                    //This isn't thread safe, I've assumed that the singleton has already been created...
                    IBusTimeTable[]? temp = Array.ConvertAll((await new BusService(ServiceId, Company.ReadingBuses).GetTimeTable(date)), item => (RbTimeTable)item);
                    temp = await FilterOutInvalidData(temp);
                    CacheWriter.WriteToCache(fileLoc, fileName, JsonConvert.SerializeObject(temp, Formatting.Indented));
                    return temp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to cache  " + ServiceId + " to cache : " + ex.Message);
                    //I write back to the cache null so that in future I do not make the same request again and keep getting null.
                    CacheWriter.WriteToCache(fileLoc, fileName, null);
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return "Service " + ServiceId;
        }


        public override int GetHashCode()
        {
            return ServiceId.GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            return Equals(obj as RbBusService);
        }

        public bool Equals(RbBusService? other)
        {
            return string.Equals(ServiceId, other?.ServiceId, StringComparison.OrdinalIgnoreCase);
        }


        public static explicit operator RbBusService(BusService service)
        {
            RbBusService newService = new()
            {
                ServiceId = service.ServiceId
            };
            return newService;
        }
    }
}
