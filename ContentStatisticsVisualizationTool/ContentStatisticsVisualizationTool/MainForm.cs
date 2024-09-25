using Eto.Drawing;
using Eto.Forms;
using Microsoft.VisualBasic.FileIO;
using ScottPlot.Eto;
using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

namespace ContentStatisticsVisualizationTool
{
    public partial class MainForm : Form
    {
        private EtoPlot ProcessorTypesPlotBar = new EtoPlot();
        private EtoPlot ContentTypesPlotBar = new EtoPlot();
        private EtoPlot DestinationFilesSizesPlotPie = new EtoPlot();
        private EtoPlot PlotScatter = new EtoPlot();

        public string fileName = "File1.mgstats";

        public MainForm()
        {
            List<string[]> allLinesAndValues = ReadStatsFile();

            DrawPlots(allLinesAndValues);

            InitializeComponent();
        }

        private void DrawPlots(List<string[]> allLinesAndValues)
        {
            DrawProcessorTypes(allLinesAndValues);

            DrawContentTypes(allLinesAndValues);

            DrawDestinationFilesSizes(allLinesAndValues);

            DrawBuildSeconds(allLinesAndValues);
        }

        private List<string[]> ReadStatsFile()
        {
            List<string[]> allLinesAndValues = new List<string[]>();

            // TODO Add error checking an Exception-Handling
            using (TextFieldParser parser = new TextFieldParser(fileName))
            {
                parser.Delimiters = new string[] { "," };
                parser.HasFieldsEnclosedInQuotes = true;

                // Read headline
                // TODO Use header-line to be more flexible when working with columns
                parser.ReadFields();

                while (parser.EndOfData == false)
                {
                    allLinesAndValues.Add(parser.ReadFields());
                }
            }

            return allLinesAndValues;
        }

        private void InitializeComponent()
        {
            Title = "Content Statistics Visualization Tool";

            MinimumSize = new Size(800, 600);

            Content = new TableLayout
            {
                Spacing = new Size(5, 5), // space between each cell
                Padding = new Padding(10, 10, 10, 10), // space around the table's sides

                Rows =
                {
                 new TableRow(
                                new TableCell( DestinationFilesSizesPlotPie,true),
                                new TableCell( ProcessorTypesPlotBar,true),
                                new TableCell(ContentTypesPlotBar, true),
                                new TableCell(PlotScatter, true)
                            )
                }
            };

            Command openFile = new Command { MenuText = "Open File", ToolBarText = "Open File" };
            openFile.Executed += (sender, e) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                DialogResult dialogResult = openFileDialog.ShowDialog(this);

                if (dialogResult == DialogResult.Ok)
                {
                    ProcessorTypesPlotBar = new EtoPlot();

                    // TODO Update title rahter than append new filename
                    Title += Path.GetFileName(openFileDialog.FileName);
                    fileName = openFileDialog.FileName;
                    // TODO The plots wont update yet
                    DrawPlots(ReadStatsFile());
                }
            };

            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem { Text = "&File", Items = { openFile } },
                }
            };

            Maximize();
        }

        private void DrawBuildSeconds(List<string[]> allLinesAndValues)
        {
            Dictionary<string, double> buildSeconds = new Dictionary<string, double>();

            foreach (string[] line in allLinesAndValues)
            {
                // TODO get rid of the magic number
                string contentType = line[3];

                double seconds = Convert.ToDouble(line[6], CultureInfo.InvariantCulture);

                if (buildSeconds.ContainsKey(contentType) == false)
                {
                    buildSeconds.Add(contentType, seconds);
                }
                else
                {
                    buildSeconds[contentType] += seconds;
                }
            }

            ScottPlot.TickGenerators.NumericManual tickGen = new();

            List<int> xs = Enumerable.Range(0, buildSeconds.Count).ToList();
            List<double> ys = new List<double>();

            int x = 0;
            foreach (var kvp in buildSeconds)
            {
                tickGen.AddMajor(x++, kvp.Key);
                ys.Add(kvp.Value);
            }

            PlotScatter.Plot.Clear();

            PlotScatter.Plot.Add.Scatter(xs, ys);

            // Labels on the bottom
            PlotScatter.Plot.Axes.Bottom.TickGenerator = tickGen;
            // Limit Y (0 and Max Value + 10%)
            PlotScatter.Plot.Axes.SetLimitsY(0, buildSeconds.Values.Max() * 1.1);

            PlotScatter.Plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
            PlotScatter.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;

            PlotScatter.Plot.Add.Annotation("Build Seconds");
        }

        private void DrawDestinationFilesSizes(List<string[]> allLinesAndValues)
        {
            Dictionary<string, double> destFileSizes = new Dictionary<string, double>();

            foreach (string[] line in allLinesAndValues)
            {
                // TODO get rid of the magic number
                string contentType = line[3];

                double destFileSize = Convert.ToDouble(line[5]);

                if (destFileSizes.ContainsKey(contentType) == false)
                {
                    destFileSizes.Add(contentType, destFileSize);
                }
                else
                {
                    destFileSizes[contentType] += destFileSize;
                }
            }

            double totalSize = destFileSizes.Values.Sum();

            List<PieSlice> slices = new List<PieSlice>();

            // Todo Add more colors/get colors differntly
            ScottPlot.Color[] colors = new ScottPlot.Color[] { ScottPlot.Colors.Red, ScottPlot.Colors.Blue, ScottPlot.Colors.Green, ScottPlot.Colors.Yellow, ScottPlot.Colors.Magenta, ScottPlot.Colors.Orange };

            int colorIndex = 0;

            foreach (string contentType in destFileSizes.Keys)
            {
                double percentValue = destFileSizes[contentType] / (totalSize / 100);

                slices.Add(new PieSlice { Label = contentType + " " + percentValue.ToString("0.00") + " %", FillColor = colors[colorIndex++], Value = destFileSizes[contentType] / (totalSize / 100) });

                // Todo get rid of that in the future
                if (colorIndex == colors.Length)
                {
                    colorIndex = 0;
                }
            }

            // setup the pie to display slice labels
            ScottPlot.Plottables.Pie pie = DestinationFilesSizesPlotPie.Plot.Add.Pie(slices);
            pie.ExplodeFraction = 0.1;
            pie.ShowSliceLabels = true;
            pie.SliceLabelDistance = 1.3;
            DestinationFilesSizesPlotPie.Plot.HideAxesAndGrid();

            DestinationFilesSizesPlotPie.Plot.Add.Annotation("Destination Files Sizes");
        }

        private void DrawContentTypes(List<string[]> allLinesAndValues)
        {
            // TODO get rid of the magic number
            var contentTypes = allLinesAndValues.SelectMany(line => line.Skip(3).Take(1));

            var contentTypeGroups = contentTypes.GroupBy(c => c);

            double[] countOfContentTypes = contentTypeGroups.Select(group => (double)group.Count()).ToArray();

            string[] labels = contentTypeGroups.Select(group => group.Key).ToArray();

            ContentTypesPlotBar.Plot.Add.Bars(countOfContentTypes);

            // Labels on the bottom
            ScottPlot.TickGenerators.NumericManual tickGen = new();

            for (int i = 0; i < labels.Length; i++)
            {
                tickGen.AddMajor(i, labels[i]);
            }

            ContentTypesPlotBar.Plot.Axes.Bottom.TickGenerator = tickGen;

            ContentTypesPlotBar.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);

            ContentTypesPlotBar.Plot.Grid.XAxisStyle.IsVisible = false;
            ContentTypesPlotBar.Plot.Grid.YAxisStyle.IsVisible = true;

            ContentTypesPlotBar.Plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
            ContentTypesPlotBar.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;

            ContentTypesPlotBar.Plot.Axes.SetLimitsY(0, countOfContentTypes.Max() + 1);

            ContentTypesPlotBar.Plot.Add.Annotation("Count of Content Type");
        }

        private void DrawProcessorTypes(List<string[]> allLinesAndValues)
        {
            // TODO get rid of the magic number
            var processorTypes = allLinesAndValues.SelectMany(line => line.Skip(2).Take(1));

            var processorTypeGroups = processorTypes.GroupBy(pt => pt);

            double[] countOfProcessorTypes = processorTypeGroups.Select(group => (double)group.Count()).ToArray();

            string[] labels = processorTypeGroups.Select(group => group.Key).ToArray();

            ProcessorTypesPlotBar.Plot.Add.Bars(countOfProcessorTypes);

            // Create labels on the bottom

            // create a tick for each bar
            Tick[] ticks = new Tick[countOfProcessorTypes.Length];
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i] = new Tick(i, labels[i]);
            }

            ProcessorTypesPlotBar.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

            //ScottPlot.TickGenerators.NumericManual tickGen = new();

            //for (int i = 0; i < labels.Length; i++)
            //{
            //    tickGen.AddMajor(i, labels[i]);
            //}

            //ProcessorTypesPlotBar.Plot.Axes.Bottom.TickGenerator = tickGen;

            // determine the width of the largest tick label
            float largestLabelWidth = 0;
            using SKPaint paint = new();
            foreach (Tick tick in ticks)
            {
                PixelSize size = ProcessorTypesPlotBar.Plot.Axes.Bottom.TickLabelStyle.Measure(tick.Label, paint).Size;
                largestLabelWidth = Math.Max(largestLabelWidth, size.Width);
            }

            // ensure axis panels do not get smaller than the largest label
            ProcessorTypesPlotBar.Plot.Axes.Bottom.MinimumSize = largestLabelWidth;
            ProcessorTypesPlotBar.Plot.Axes.Right.MinimumSize = largestLabelWidth;

            ProcessorTypesPlotBar.Plot.Axes.Margins(bottom: 0);

            ProcessorTypesPlotBar.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);

            ProcessorTypesPlotBar.Plot.Grid.XAxisStyle.IsVisible = false;
            ProcessorTypesPlotBar.Plot.Grid.YAxisStyle.IsVisible = true;

            ProcessorTypesPlotBar.Plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
            ProcessorTypesPlotBar.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;

            ProcessorTypesPlotBar.Plot.Axes.SetLimitsY(0, countOfProcessorTypes.Max() + 1);

            ProcessorTypesPlotBar.Plot.Add.Annotation("Count of Processor Type");
        }
    }
}