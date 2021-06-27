// Copyright (c) Joanthan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0

using System;
using System.Threading.Tasks;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace TOR_Tests.Stubs
{
    public class ServiceStub : IBusService
    {
        public string ServiceId { get; }

        public ServiceStub(string serviceId)
        {
            ServiceId = serviceId;
        }


        public Task<IBusStop[]> GetLocations(Direction direction)
        {
            throw new NotImplementedException();
        }

        public Task<IBusTimeTable[]> GetTimeTable(DateTime date)
        {
            throw new NotImplementedException();
        }

        public bool IsTimeTableCached(DateTime date)
        {
            throw new NotImplementedException();
        }

        public Task<IBusHistoricTimeTable[]> GetArchivedTimeTable(DateTime date)
        {
            throw new NotImplementedException();
        }

        public bool IsArchivedTimeTableCached(DateTime date)
        {
            throw new NotImplementedException();
        }

        public bool IsWeakServiceSame(IBusService service)
        {
            throw new NotImplementedException();
        }
    }
}
