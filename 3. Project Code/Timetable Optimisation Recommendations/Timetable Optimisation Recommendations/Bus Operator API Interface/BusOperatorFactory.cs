// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Threading.Tasks;
using System.Windows;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface.ReadingBuses;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{
    /// <summary>
    /// A singleton factory object, that can produce and return back the current IBusOperator object.
    /// This allows for support of several bus operators, not just Reading Buses. You would set the operator
    /// you want to get data for and the factory will then return a singleton reference to the operator object.
    /// </summary>
    public sealed class BusOperatorFactory
    {
        /// <summary>
        /// Provides a thread safe and lazy way to generate a singleton factory object.
        /// </summary>
        private static readonly Lazy<BusOperatorFactory> Lazy =  new(() => new BusOperatorFactory());

        /// <summary>
        /// Used to request an instance to the singleton object.
        /// </summary>
        public static BusOperatorFactory Instance { get { return Lazy.Value; } }

        /// <summary>
        /// Stores the currently selected operator to return.
        /// </summary>
        private volatile Operators _operatorSelected;

        /// <summary>
        /// Stores the object reference for the current operator.
        /// </summary>
        public IBusOperator Operator { get; private set; } = new StubOperator();

        /// <summary>
        /// Private constructor to prevent outside initialing the object and enforcing the singleton pattern.
        /// </summary>
        private BusOperatorFactory()
        {
        }

        /// <summary>
        /// Used to switch between operators to return. 
        /// </summary>
        /// <param name="selected">The bus operator to return</param>
        /// <returns>Nothing, signifies once completed.</returns>
        public async Task SetOperatorAsync(Operators selected)
        {
            try
            {
                _operatorSelected = selected;

                switch (_operatorSelected)
                {
                    case Operators.ReadingBuses:
                        Operator = await RbBusOperator.Initialise(Properties.Settings.Default.API_KEY);
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("The Reading Buses API Has been turned off, you must use the cache data." + Environment.NewLine + "Please contact me for access to the data source to run the program:" + Environment.NewLine + "psyjpf@nottingham.ac.uk", "API Unable To Connect", MessageBoxButton.OK,MessageBoxImage.Error);
                Operator = new StubOperator();
            }
         
        }
    }
}
