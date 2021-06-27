// Copyright (c) Jonathan Foot. All Rights Reserved. 
// Licensed under the GNU Affero General Public License, Version 3.0 
// See the LICENSE file in the project root for more information.

using System;

namespace ReadingBusesAPI.Common
{
	/// <summary>
	///     Stores an X and Y Position simply.
	/// </summary>
	public readonly struct Point : IEquatable<Point>
	{
		/// <value>The X value.</value>
		public double X { get; }

		/// <value>The Y Value.</value>
		public double Y { get; }

		/// <summary>
		///     Default constructor
		/// </summary>
		/// <param name="x">X value of Point.</param>
		/// <param name="y">Y value of Point.</param>
		public Point(double x, double y) : this()
		{
			X = x;
			Y = y;
		}

		/// <summary>
		///     Converts point to string representation.
		/// </summary>
		/// <returns>Point as a string.</returns>
		public override string ToString()
		{
			return X + ", " + Y;
		}


		#region isEqualsOrNot

		/// <summary>
		///     Generates a unique number for each Point Object.
		/// </summary>
		/// <returns>Int value of object.</returns>
		public override int GetHashCode()
		{
			return (int)Math.Pow(X, Y);
		}

		/// <summary>
		///     Checks if two point objects are the same or not.
		/// </summary>
		/// <param name="obj">Another object to compare against this object.</param>
		/// <returns>Is it the same object or not.</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Point))
			{
				return false;
			}

			return Equals((Point)obj);
		}

		/// <summary>
		///     Implements logic for checking if two objects are equal.
		/// </summary>
		/// <param name="other">The other object to check if equal.</param>
		/// <returns>True if equals else false.</returns>
		public bool Equals(Point other) => Y.Equals(other.Y) && X.Equals(other.X);

		/// <summary>
		///     Checks if two objects are the same.
		/// </summary>
		/// <param name="point1">First Point Object.</param>
		/// <param name="point2">Second Point Object.</param>
		/// <returns>True if equal else false</returns>
		public static bool operator ==(Point point1, Point point2)
		{
			return point1.Equals(point2);
		}

		/// <summary>
		///     Checks if two objects are the not the same.
		/// </summary>
		/// <param name="point1">First Point Object.</param>
		/// <param name="point2">Second Point Object.</param>
		/// <returns>True if they are not the same.</returns>
		public static bool operator !=(Point point1, Point point2)
		{
			return !point1.Equals(point2);
		}

		#endregion
	};
}
