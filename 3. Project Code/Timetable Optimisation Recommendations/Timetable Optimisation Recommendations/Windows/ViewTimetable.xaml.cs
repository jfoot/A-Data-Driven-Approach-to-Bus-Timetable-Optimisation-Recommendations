// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;

namespace Timetable_Optimisation_Recommendations.Windows
{
    /// <summary>
    /// Used to display the timetable to the user.
    /// </summary>
    public partial class ViewTimetable : Window
    {
        ///<value>The timetable to display.</value>
        private readonly IBusTimeTable[] _records;
        ///<value>The service the timetable pertains too.</value>
        private readonly IBusService _service;

        /// <summary>
        /// The default constructor for the timetable viewer.
        /// </summary>
        /// <param name="records">The timetable to display.</param>
        /// <param name="service">The service it is about.</param>
        public ViewTimetable(IBusTimeTable[] records, IBusService service)
        {
            InitializeComponent();
            _records = records;
            _service = service;
        }

        /// <summary>
        /// Upon finishing loading the window start filing the table with data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WindowLoadedAsync(object sender, RoutedEventArgs e)
        {
            ServiceTitle.Content = "Service " + _service.ServiceId;
            await FillTable(Direction.Outbound, TimeTableGridOutBound);
            await FillTable(Direction.Inbound, TimeTableGridInBound);
        }


        /// <summary>
        /// Actually fills the gird up with data.
        /// </summary>
        /// <param name="direction">The direction of travel of the service.</param>
        /// <param name="grid">Which data grid to populate with data.</param>
        /// <returns></returns>
        private async Task FillTable(Direction direction, DataGrid grid)
        {
            //Used to store the information inside the data grid.
            DataTable table = new();

            //Gets all the bus stops in the direction of travel.
            IBusStop[] locations = await _service.GetLocations(direction);

            //Group timetable records by journey 
            IGrouping<string, IBusTimeTable>[] groupByJourney = _records.Where(rs => rs.MatchDirection(direction)).GroupBy(r => r.JourneyCode).ToArray();

            //Add in the columns, the stop name is always first, followed by ordered journeys.
            table.Columns.Add("Stop Name", typeof(string));
            foreach (IGrouping<string, IBusTimeTable> journey in groupByJourney)
                table.Columns.Add(journey.Key, typeof(string));



            //Keeps track of the row/y value.
            int y = 0;
            //For each location, which makes up each row of data.
            foreach (IBusStop location in locations)
            {
                //Store each column for the row as an array.
                string[] rowData = new string[table.Columns.Count];
                //First column is always the stop name
                rowData[0] = location.CommonName;


                //Keeps track of the column/x value 
                int x = 1;
                //For each journey that makes up each column.
                foreach (IGrouping<string, IBusTimeTable> journeyGroup in groupByJourney)
                {
                    //Checks if a bus timetable record exists, ie on this Journey does it visit this stop.
                    IBusTimeTable? record = journeyGroup.FirstOrDefault(r => r.WeakIsStopSame(location));

                    //If it does visit the stop.
                    if (record != null)
                    {
                        //Sets the cell in the row to be arrival time.
                        rowData[x] = record.SchArrivalTime.ToString("HH:mm");
                    }
                    else
                    {
                        rowData[x] = "-";
                    }

                    //increment X for next iteration.
                    ++x;
                }

                ++y;
                //Add this row to the data table.
                table.Rows.Add(rowData);
            }

            //Set the data grid the data table, updating to the user.
            grid.DataContext = table.DefaultView;
        }
    }
}
