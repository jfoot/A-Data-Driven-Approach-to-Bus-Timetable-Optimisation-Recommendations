// Copyright (c) Joanthan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0

using Newtonsoft.Json;
using ReadingBusesAPI.Common;

namespace ReadingBusesAPI.BusServices
{
    internal class StopPatteren
    {
        /// <value>The unique identifier for a bus stop.</value>
        [JsonProperty("location_code")]
        public string ActoCode { get; internal set; }

        /// <value>The public, easy to understand stop name.</value>
        [JsonProperty("location_name")]
        public string CommonName { get; internal set; }

        /// <value>The operator associated to the record, this is needed so you can filter it out.</value>
        [JsonConverter(typeof(ParseOperatorConverter))]
        [JsonProperty("operator_code")]
        public Company OperatorCode { get; internal set; }


        /// <value>The order in which a stop is visited.</value>
        [JsonProperty("display_order")]
        public int Order { get; internal set; }


#pragma warning disable 0649
        /// <value>The direction of travel.</value>
        [JsonProperty("direction")] 
        private readonly int _direction;
#pragma warning restore 0649 


        public bool IsOutbound()
        {
            return _direction == 0;
        }
    }
}
