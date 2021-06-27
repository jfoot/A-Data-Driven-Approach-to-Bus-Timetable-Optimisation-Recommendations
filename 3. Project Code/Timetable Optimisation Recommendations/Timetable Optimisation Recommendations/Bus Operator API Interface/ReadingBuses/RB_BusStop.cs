// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReadingBusesAPI.BusStops;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses
{
    /// <summary>
    /// An Implementation of the IBusStop interface for the Reading Buses API.
    /// </summary>
    public sealed class RbBusStop : IBusStop
    {
        [JsonProperty] public string AtcoCode { get; private set; } = string.Empty;
        [JsonProperty] public string CommonName { get; private set; } = string.Empty;
        [JsonProperty] public string Latitude { get; private set; } = string.Empty;
        [JsonProperty] public string Longitude { get; private set; } = string.Empty;
        [JsonProperty] public string Bearing { get; private set; } = string.Empty;
        [JsonProperty] public string[]? Services { get; private set; }

        //Used to store the references to the bus services object, this is done lazily so not to hog resources. 
        [JsonIgnore] private IBusService[]? _servicesObj;


        [JsonIgnore] private static readonly InternalCache<IBusHistoricTimeTable[]> ArchivedTimeTableCache = new();

        public IBusService[] GetServices()
        {
            if (_servicesObj == null)
            {
                List<IBusService> servicesTemp = new();

                //Go through each service and 
                foreach (string service in Services ?? Array.Empty<string>())
                    if (RbBusOperator.GetInstance().IsService(service))
                        servicesTemp.Add(RbBusOperator.GetInstance().GetService(service));

                _servicesObj = servicesTemp.ToArray();
            }

            return _servicesObj;
        }

        public bool IsArchivedTimeTableCached(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTableStop/" + AtcoCode + "/Historic/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;

            return File.Exists(fqn);
        }


        

        public async Task<IBusHistoricTimeTable[]?> GetArchivedTimeTable(DateTime date)
        {
            return await ArchivedTimeTableCache.GetOrCreate(date.ToShortDateString() + AtcoCode, async () => await FindArchivedTimeTable(date));
        }

        private async Task<IBusHistoricTimeTable[]?> FindArchivedTimeTable(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTableStop/" + AtcoCode + "/Historic/";
            string fileName = date.ToShortDateString().Replace('/', '-') + ".cache";
            string fqn = fileLoc + "/" + fileName;

            if (File.Exists(fqn))
            {
                try
                {
                    return JsonConvert.DeserializeObject<RbTimeTableHistoric[]>(File.ReadAllText(fqn));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Find Archived Bus Stop Cache Timetable Failed : " + ex.Message);
                    File.Delete(fqn);
                    return await GetArchivedTimeTable(date);
                }
            }
            else
            {
                try
                {
                    //Request the archived time table data the bus stop.
                    var raw = (await new BusStop(AtcoCode).GetArchivedTimeTable(date)).ToList();
                    //Then go through the list and check that all services found are also in our data store (this removes any none RB services from the data-set)
                    for (int i = raw.Count - 1; i >= 0; i--)
                        if (!RbBusOperator.GetInstance().IsService(raw[i].GetService().ServiceId))
                            raw.RemoveAt(i);

                    //Then convert all the objects into the correct format and save to cache.
                    IBusHistoricTimeTable[] temp = Array.ConvertAll(raw.ToArray(), item => (RbTimeTableHistoric)item);
                    CacheWriter.WriteToCache(fileLoc, fileName, JsonConvert.SerializeObject(temp, Formatting.Indented));
                    return temp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BusStop Error : " + ex.Message);
                    //I write back to the cache null so that in future I do not make the same request again and keep getting null.
                    CacheWriter.WriteToCache(fileLoc, fileName, null);
                    return null;
                }
            }
        }



        public async Task<IBusHistoricTimeTable[]?> GetWeakArchivedTimeTable(DateTime date)
        {
            return await ArchivedTimeTableCache.GetOrCreate(date.ToShortDateString() + "WEAK" + AtcoCode, async () => await FindWeakArchivedTimeTable(date));
        }

        private async Task<IBusHistoricTimeTable[]?> FindWeakArchivedTimeTable(DateTime date)
        {
            string fileLoc = RbBusOperator.CacheDirectory + "/TimeTableStop/" + AtcoCode + "/Historic/";
            string fileName = date.ToShortDateString().Replace('/', '-') + "-WEAK.cache";
            string fqn = fileLoc + "/" + fileName;
           
            if (File.Exists(fqn))
            {
                try
                {
                    return JsonConvert.DeserializeObject<RbTimeTableHistoric[]>(File.ReadAllText(fqn));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Find Weak Archived Bus Stop Cache Timetable Failed : " + ex.Message);
                    File.Delete(fqn);
                    return await GetWeakArchivedTimeTable(date);
                }
            }
            else
            {
                try
                {
                    List<IBusHistoricTimeTable> timetables = new();
            
                    IBusService[] services = GetServices();
                    foreach(IBusService? service in services)
                    {
                        if (service.IsArchivedTimeTableCached(date))
                        {
                            IBusHistoricTimeTable[] timeTable = await service.GetArchivedTimeTable(date) ?? Array.Empty<IBusHistoricTimeTable>();
                            timetables.AddRange(timeTable.Where(record => record.Location == this).ToArray());
                        }
                    }

                    IBusHistoricTimeTable[]? result = timetables.OrderBy(record => record.SchArrivalTime).ToArray();

                    CacheWriter.WriteToCache(fileLoc, fileName, JsonConvert.SerializeObject(result, Formatting.Indented));
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("BusStop Error : " + ex.Message);
                    //I write back to the cache null so that in future I do not make the same request again and keep getting null.
                    CacheWriter.WriteToCache(fileLoc, fileName, null);
                    return null;
                }
            }
        }



        public override bool Equals(object? obj)
        {
            return Equals(obj as RbBusStop);
        }

        public bool Equals(RbBusStop? other)
        {
            return string.Equals(AtcoCode, other?.AtcoCode, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return AtcoCode.GetHashCode();
        }


        public override string ToString()
        {
            return CommonName + " (" + AtcoCode + ")";
        }


        public static explicit operator RbBusStop(BusStop stop)
        {
            return new()
            {
                AtcoCode = stop.AtcoCode,
                Bearing = stop.Bearing,
                CommonName = stop.CommonName,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Services = stop.ServicesString.Split('/')
            }; 
        }
    }
}
