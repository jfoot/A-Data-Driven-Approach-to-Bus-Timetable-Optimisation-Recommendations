// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface;
using Timetable_Optimisation_Recommendations.Search_Algorithm.Tabu_Search;
using Timetable_Optimisation_Recommendations.Timetable_Evaluator;

namespace Timetable_Optimisation_Recommendations.Windows
{
    /// <summary>
    /// Interaction logic for ViewTimetable.xaml
    /// </summary>
    public partial class ViewTimetableHighlighted : Window
    {
        ///<value>The timetable to display.</value>
        private readonly BlamedBusTimeTable[] _records;
        ///<value>The service the timetable pertains too.</value>
        private readonly IBusService _service;
        ///<value>The moves made by the search algorithm</value>
        private readonly Move[]? _moves;
        ///<value>Stores a list of highlights for every cell.</value>
        private readonly List<Highlight> _highlights = new();

        private readonly string _percentageChange;

        ///<value>Stores the RGB max and min for the cell highlighting.</value>
        private const int RGB_MAX = 255; 
        private const int RGB_MIN = 0;


        /// <summary>
        /// The default constructor for the view timetable highlights window.
        /// </summary>
        /// <param name="records">The timetable with blame values to display and highlight.</param>
        /// <param name="service">The service it pertains too.</param>
        /// <param name="percentageChange">The percentage improvement compared to the original timetable.</param>
        /// <param name="moves">The moves that the search algorithm made.</param>
        public ViewTimetableHighlighted(BlamedBusTimeTable[] records, IBusService service, string percentageChange, Move[]? moves = null)
        {
            InitializeComponent();
            _records = records;
            _service = service;
            _moves = moves;
            _percentageChange = percentageChange;
        }

        /// <summary>
        /// Upon finishing loading the window start filing the table with data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WindowLoadedAsync(object sender, RoutedEventArgs e)
        {
            ServiceTitle.Content = "Service " + _service.ServiceId + " Improved : " + _percentageChange + "%";
            await FillTable(Direction.Outbound, TimeTableGridOutBound);
            await FillTable(Direction.Inbound, TimeTableGridInBound);
        }

        

        /// <summary>
        /// This code was heavily inspired from code found on stack overflow here.
        /// https://stackoverflow.com/a/27901262/10115963
        /// Author - Mark Whitaker
        ///
        /// Given a percentage value returns a shade between red and green.
        /// </summary>
        /// <param name="percentage">Given an integer percentage from 0 to 100</param>
        /// <returns>A colour where red = 0 and green = 100</returns>
        private static SolidColorBrush GetColorFromPercentage(int percentage)
        {
            // Work out the percentage of red and green to use (i.e. a percentage
            // of the range from RGB_MIN to RGB_MAX)
            float redPercent = Math.Min(percentage * 2, 100) / 100f;
            float greenPercent = Math.Min(200 - (percentage * 2), 100) / 100f;

            // Now convert those percentages to actual RGB values in the range
            // RGB_MIN - RGB_MAX
            float red = RGB_MIN + ((RGB_MAX - RGB_MIN) * redPercent);
            float green = RGB_MIN + ((RGB_MAX - RGB_MIN) * greenPercent);

            return new SolidColorBrush(Color.FromRgb((byte)red, (byte)green, RGB_MIN));
        }

        /// <summary>
        /// Given a record returns back the colour the cell should be based upon it's total blame value.
        /// </summary>
        /// <param name="record">A blamed record.</param>
        /// <returns>The colour the associated cell should be.</returns>
        private SolidColorBrush PercentageTotal(BlamedBusTimeTable record)
        {
            double maxTotWeight = _records.Max(rec => rec.TotalWeight);
            double minTotWeight = _records.Min(rec => rec.TotalWeight);
            double rangeTot = maxTotWeight - minTotWeight;

            return GetColorFromPercentage((int)(((record.TotalWeight - minTotWeight) / rangeTot) * 100));
        }

        /// <summary>
        ///  Given a record returns back the colour the cell should be based upon it's slack blame value.
        /// </summary>
        /// <param name="record">A blamed record.</param>
        /// <returns>The colour the associated cell should be based upon slack blame.</returns>
        private static SolidColorBrush PercentageSlack(BlamedBusTimeTable record)
        {
            return GetColorFromPercentage((int)((record.SlackWeights.Weight ?? 0) * 100));
        }

        /// <summary>
        ///  Given a record returns back the colour the cell should be based upon it's cohesion blame value.
        /// </summary>
        /// <param name="record">A blamed record.</param>
        /// <returns>The colour the associated cell should be based upon cohesion blame.</returns>
        private static SolidColorBrush PercentageCohesion(BlamedBusTimeTable record)
        {
            return GetColorFromPercentage((int)((record.CohesionWeights.Weight ?? 0.5) * 100));
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
            IGrouping<string, BlamedBusTimeTable>[] groupByJourney = _records.Where(rs => rs.MatchDirection(direction)).GroupBy(r => r.JourneyCode).ToArray();

            //Add in the columns, the stop name is always first, followed by ordered journeys.
            table.Columns.Add("Stop Name", typeof(string));
            foreach (IGrouping<string, BlamedBusTimeTable> journey in groupByJourney)
                table.Columns.Add(journey.Key.ToString(), typeof(string));



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
                foreach (IGrouping<string, BlamedBusTimeTable> journeyGroup in groupByJourney)
                {
                    //Checks if a bus timetable record exists, ie on this Journey does it visit this stop.
                    BlamedBusTimeTable? record = journeyGroup.FirstOrDefault(r => r.WeakIsStopSame(location));

                    //If it does visit the stop.
                    if (record != null)
                    {
                        //Sets the cell in the row to be arrival time.
                        rowData[x] = record.SchArrivalTime.ToString("HH:mm");

                        //rowData[x] = record.SlackWeights.Weight?.ToString("0.00") ?? "-";

                        _highlights.Add(new Highlight()
                        {
                            X = x,
                            Y = y,
                            TotalWeight = PercentageTotal(record),
                            SlackWeight = PercentageSlack(record),
                            CohesionWeight = PercentageCohesion(record),
                            IsOutbound = record.IsOutbound
                        });



                        //Checks if there is any fix made at this cell.
                        if (_moves != null && _moves.Any(mv => mv.ChangedRecordsIDs.Contains(record.GetId())))
                        {
                            _highlights.Last().MoveHighlight = Brushes.CornflowerBlue;
                        }
                        

                        //Checks if there is any move made at this cell.
                        if (_moves != null && _moves.Any(mv => mv.TargetRecord.GetId() == record.GetId()))
                        {
                            _highlights.Last().MoveHighlight = Brushes.Red;
                        }
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

        /// <summary>
        /// Highlights a specific cell a specific colour/
        /// </summary>
        /// <param name="columnIndex">The X coordinate.</param>
        /// <param name="rowIndex">The Y coordinate.</param>
        /// <param name="grid">The grid to colour.</param>
        /// <param name="color">What colour to colour it.</param>
        private static void HighlightCell(int columnIndex, int rowIndex, DataGrid grid, SolidColorBrush color)
        {
            DataGridRow? row = GetRow(grid, rowIndex);
            if (row != null)
            {
                DataGridCell? cell = GetCell(grid, row, columnIndex);
                if (cell != null)
                    cell.Background = color;
            }
        }

        /// <summary>
        /// Gets the row from the grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="rowIndex">The Y index of row to get.</param>
        /// <returns>The whole row at the index, null if not found.</returns>
        private static DataGridRow? GetRow(DataGrid grid, int rowIndex)
        {
            if (grid.Items.Count <= rowIndex)
                return null;

            return grid.ItemContainerGenerator.ContainerFromItem(grid.Items[rowIndex]) as DataGridRow;
        }

        /// <summary>
        /// Gets the cell from the row.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="row">The Y index of row to get.</param>
        /// <param name="columnIndex">The X index within the row.</param>
        /// <returns>The cell at the indexs, null if not found.</returns>
        private static DataGridCell? GetCell(DataGrid grid, DataGridRow? row, int columnIndex)
        {
            if (row == null || grid.Columns.Count <= columnIndex)
                return null;

            return grid.Columns[columnIndex].GetCellContent(row)?.Parent as DataGridCell;
        }

        /// <summary>
        /// Called upon when the user clicks the save to CSV button.
        /// Saves the current timetable to a CSV file for inspection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveToCsv(object sender, RoutedEventArgs e)
        {
            SaveFileDialog save = new()
            {
                Filter = "Microsoft Excel Comma Separated Values|*.csv",
                Title = "Export TimeTable"
            };

            if (save.ShowDialog() ?? false)
            {
                string complete =
                    "Outbound" + Environment.NewLine +
                    DataGridToCsv(TimeTableGridOutBound) + Environment.NewLine +
                    "Inbound" + Environment.NewLine +
                    DataGridToCsv(TimeTableGridInBound);

                await File.WriteAllTextAsync(save.FileName, complete);
            }
        }

        /// <summary>
        /// Converts the data-grid into a CSV format for saving.
        /// </summary>
        /// <param name="data">The data grid in question to convert.</param>
        /// <returns>The string representation of the grid.</returns>
        private static string DataGridToCsv(DataGrid data)
        {
            DataTable dt = ((DataView)data.ItemsSource).ToTable();

            StringBuilder stringBuilder = new();

            foreach (DataColumn column in dt.Columns)
                stringBuilder.Append(column.ColumnName + ",");

            stringBuilder.Append(Environment.NewLine);

            foreach (DataRow row in dt.Rows)
                stringBuilder.Append(string.Join(",", row.ItemArray) + Environment.NewLine);

            return stringBuilder.ToString();
        }


        /// <summary>
        /// All of the below code is used to trigger different forums of highlighting on different grids.
        /// </summary>





        private void HighlightMovesInbound(object sender, RoutedEventArgs e)
        {
            ClearInBound();
            foreach (Highlight highlight in _highlights.Where(rec => !rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridInBound, highlight.MoveHighlight);
        }

        private void HighlightMovesOutbound(object sender, RoutedEventArgs e)
        {
            ClearOutbound();
            foreach (Highlight highlight in _highlights.Where(rec => rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridOutBound, highlight.MoveHighlight);
        }


        private void ClearOutbound()
        {
            foreach (Highlight highlight in _highlights.Where(rec => rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridOutBound, new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)));
        }

        private void ClearInBound()
        {
            foreach (Highlight highlight in _highlights.Where(rec => !rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridInBound, new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)));
        }

        private void ClearOutbound(object sender, RoutedEventArgs e)
        {
            ClearOutbound();
        }

        private void HighlightSlackOutbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridOutBound, highlight.SlackWeight);
        }

        private void HighlightCohesionOutbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridOutBound, highlight.CohesionWeight);
        }

        private void HighlightTotalOutbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridOutBound, highlight.TotalWeight);
        }

        private void HighlightTotalInbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => !rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridInBound, highlight.TotalWeight);
        }

        private void HighlightCohesionInbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => !rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridInBound, highlight.CohesionWeight);
        }

        private void HighlightSlackInbound(object sender, RoutedEventArgs e)
        {
            foreach (Highlight highlight in _highlights.Where(rec => !rec.IsOutbound))
                HighlightCell(highlight.X, highlight.Y, TimeTableGridInBound, highlight.SlackWeight);
        }

        private void ClearInbound(object sender, RoutedEventArgs e)
        {
            ClearInBound();
        }

        /// <summary>
        /// Called upon when the user clicks on the cell of an outbound servie.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClickOnCellOutbound(object sender, SelectedCellsChangedEventArgs e)
        {
            await DisplayInfo(TimeTableGridOutBound, Direction.Outbound);
        }

        /// <summary>
        /// Called upon when the user clicks on the cell of an inbound service. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClickOnCellInbound(object sender, SelectedCellsChangedEventArgs e)
        {
            await DisplayInfo(TimeTableGridInBound, Direction.Inbound);
        }

        /// <summary>
        /// Takes in a grid and a direction and then works out what cell has just been highlighted and then
        /// displays a message box giving more information about that record.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private async Task DisplayInfo(DataGrid grid, Direction direction)
        {
            //This finds the X and Y value of the cell that the user has click on. 
            //This is actually very difficult to do and so reflection is used.
            //This isn't a great solution, but it works.
            DataGridCellInfo currentCell = grid.CurrentCell;

            int rowIndex = int.MinValue;
            if (currentCell.Item is DataRowView rowView)
            {
                DataRow dataRow = rowView.Row;
                FieldInfo? fi = typeof(DataRow).GetField("_rowID", BindingFlags.NonPublic | BindingFlags.Instance);
                try
                {
                    if (fi != null)
                    {
                        rowIndex = Convert.ToInt32(fi.GetValue(dataRow));
                    }
                }
                catch (InvalidCastException) { }

            }

            int x2 = currentCell.Column.DisplayIndex;
            int y2 = rowIndex;


            //Group timetable records by journey 
            IGrouping<string, BlamedBusTimeTable>[] groupByJourney = _records.Where(rs => rs.MatchDirection(direction)).GroupBy(r => r.JourneyCode).ToArray();

            //Gets all the bus stops in the direction of travel.
            IBusStop[] locations = await _service.GetLocations(direction);

            //Keeps track of the row/y value.
            int y = 1;
            //For each location, which makes up each row of data.
            foreach (IBusStop location in locations)
            {

                //Keeps track of the column/x value 
                int x = 1;
                //For each journey that makes up each column.
                foreach (IGrouping<string, BlamedBusTimeTable> journeyGroup in groupByJourney)
                {
                    if (x == x2 && y == y2)
                    {
                        //Checks if a bus timetable record exists, ie on this Journey does it visit this stop.
                        BlamedBusTimeTable? record = journeyGroup.FirstOrDefault(r => r.WeakIsStopSame(location));

                        
                        MessageBox.Show("******************"
                                        + Environment.NewLine + "Service " + record?.Service.ServiceId + " at "+ record?.Location.CommonName + " JourneyCode " + record?.JourneyCode
                                        + Environment.NewLine + "Current : Scheduled Arrival - " + record?.SchArrivalTime.ToString("HH:mm:ss") + " to Scheduled Departure - " + record?.SchDepartureTime.ToString("HH:mm:ss") 
                                        + Environment.NewLine + "Slack Wants : New Arrival - " + record?.SlackWeights.TargetSchArrivalTime.ToString("HH:mm:ss") + " to Scheduled Departure - " + record?.SlackWeights.TargetSchDepartureTime.ToString("HH:mm:ss") + Environment.NewLine + "Slack Weights : Raw Weight - " + record?.SlackWeights.RawWeight?.ToString("00.000") + "  Normalized Weight - " + record?.SlackWeights.Weight?.ToString("00.000")
                                        + Environment.NewLine + "Cohesion Wants : New Arrival - " + record?.CohesionWeights.TargetSchArrivalTime.ToString("HH:mm:ss") + " to Scheduled Departure - " + record?.CohesionWeights.TargetSchDepartureTime.ToString("HH:mm:ss") + Environment.NewLine + "Cohesion Weights : Raw Weight - " + record?.CohesionWeights.RawWeight?.ToString("00.000") + "  Normalized Weight - " + record?.CohesionWeights.Weight?.ToString("00.000")
                                        + Environment.NewLine + "******************", "Record Details for Service " + record?.Service.ServiceId);
                    }
                    ++x;
                }
                ++y;
            }
        }
    }
}
