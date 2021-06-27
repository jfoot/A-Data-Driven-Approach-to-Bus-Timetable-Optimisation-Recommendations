// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// The default operator if none is selected, this is mainly used to satisfy the null-ability requirement of C#
    /// then anything else.
    /// </summary>
    public class StubOperator : IBusOperator
    {
        public void ForceUpdateCache()
        {
            throw new NotImplementedException();
        }

        public IBusStop GetLocation(string atcoCode)
        {
            throw new NotImplementedException();
        }

        public IBusService GetService(string serviceNumber)
        {
            throw new NotImplementedException();
        }

        public IBusService[] GetServices()
        {
            return Array.Empty<IBusService>();
        }

        public void InvalidateCache()
        {
            throw new NotImplementedException();
        }

        public bool IsLocation(string actoCode)
        {
            throw new NotImplementedException();
        }

        public bool IsService(string serviceNumber)
        {
            throw new NotImplementedException();
        }
    }
}
