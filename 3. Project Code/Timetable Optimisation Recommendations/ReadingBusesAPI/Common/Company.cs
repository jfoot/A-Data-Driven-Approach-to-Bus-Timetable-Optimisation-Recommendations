// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

namespace ReadingBusesAPI.Common
{
	/// <summary>
	///     An Enum of the Operators Reading Buses owns or manages in their API.
	/// </summary>
	public enum Company
	{
		/// <summary>
		///     For Reading Buses services
		/// </summary>
		ReadingBuses,

		/// <summary>
		///     For Kennections services
		/// </summary>
		Kennections,

		/// <summary>
		///     For Newbury And District services
		/// </summary>
		NewburyAndDistrict,

		/// <summary>
		///     For any other operator which is new in the API and has not yet been officially supported in this library.
		/// </summary>
		Other
	}
}
