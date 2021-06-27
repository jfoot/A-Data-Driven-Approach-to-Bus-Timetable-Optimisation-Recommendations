// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Runtime.InteropServices;
using System.Windows;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Windows;


namespace Timetable_Optimisation_Recommendations
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#pragma warning disable CA1401
#if DEBUG
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();
#endif
#pragma warning restore CA1401

        // public static IBusOperator instance;

        protected override async void OnStartup(StartupEventArgs e)
        {
            //SplashScreen.Show("Authenticating");
            //await Authenticate();
            //SplashScreen.Close();
#if DEBUG
            AllocConsole();
#endif
            await BusOperatorFactory.Instance.SetOperatorAsync(Operators.ReadingBuses);
                        
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

    }
}
