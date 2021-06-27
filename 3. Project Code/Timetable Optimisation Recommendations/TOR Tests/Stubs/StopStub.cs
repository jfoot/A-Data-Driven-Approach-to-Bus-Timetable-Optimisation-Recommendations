// Copyright (c) Joanthan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0

using System;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace TOR_Tests.Stubs
{
    public class StopStub : IBusStop
    {
        public string AtcoCode { get; set; }

        public string CommonName { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Bearing { get; set; }

        public string[] Services { get; set; }


        public StopStub(string atcoCode, string[] services)
        {
            AtcoCode = atcoCode;
            Services = services;
        }


        public Task<IBusHistoricTimeTable[]> GetArchivedTimeTable(DateTime date)
        {
            throw new NotImplementedException();
        }

        public Task<IBusHistoricTimeTable[]> GetArchivedTimeTable(DateTime date, IBusService service)
        {
            throw new NotImplementedException();
        }

        public IBusService[] GetServices()
        {
            throw new NotImplementedException();
        }

        public Task<IBusHistoricTimeTable[]> GetWeakArchivedTimeTable(DateTime date)
        {
            throw new NotImplementedException();
        }

        public bool IsArchivedTimeTableCached(DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}
