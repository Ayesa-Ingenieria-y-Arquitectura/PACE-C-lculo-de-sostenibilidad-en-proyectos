using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.WPF;
using System.Net.NetworkInformation;

namespace Bc3_WPF.Screens.Charts;

public class Pie
{
    public IEnumerable<ISeries> Series { get; set; }

    public ObservableCollection<ISeries> Series2 { get; set; }

    public LabelVisual TitleChart { get; set; }

    public LabelVisual TitlePie { get; set; }
    public List<Axis> axes { get; set; }

    public Pie()
    {
        axes = new();

        Series2 = new ObservableCollection<ISeries>
        {
            new LineSeries<decimal>
            {
                Values = new ObservableCollection<decimal> {}, // Ensure it's ObservableCollection<int>
                Fill = null,
                GeometrySize = 20
            }
        };

        this.Series = new[] { 2, 4, 1, 4, 3 }.AsPieSeries((value, series) =>
        {
            series.MaxRadialColumnWidth = 60;
        });

        TitleChart =
        new LabelVisual
        {
            Text = "Prueba 1",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15)
        };

        TitlePie = new LabelVisual
        {
            Text = "Carbono por concepto en Tabla",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15)
        };
    }

    public static void setDoughtData(Dictionary<string?, double?>? data, Pie pie)
    {
        List<double?> values = data.Select(e => e.Value).ToList();
        List<string> names = data.Select(e => e.Key).ToList();
        int _index = 0;

        pie.Series = values.AsPieSeries((value, series) =>
        {
            series.Name = names[_index++ % names.Count];
            series.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Start;
            series.DataLabelsSize = 15;
        });
    }

    public static void setDoughtTitle(string s, Pie pie)
    {
        pie.TitlePie.Text = s;
    }

    public static void updateLineChart(List<KeyValuePair<string, double?>> data, Pie pie)
    {
        List<int?> values = data.Select(e => (int?) e.Value).ToList();
        List<string> labels = data.Select(e => "Change "+e.Key).ToList();

        ObservableCollection<int> v = new ObservableCollection<int>();

        foreach (int? val in values)
        {
            if (val.HasValue)  // Filter out null values
            {
                v.Add(val.Value);
            }
        }

        var lineSeries = new LineSeries<int>
        {
            Values = v,   // ObservableCollection of decimal values
            Fill = null,  // No fill for the line
            GeometrySize = 10,
            Name = "Ayesa"// Optional: Adjust the size of data point markers
        };

        var XAxes = new List<Axis>
        {
            new Axis
            {
                Labels = labels
            }
        };

        // Clear previous series (optional depending on use case)
        pie.Series2.Clear();

        // Add the updated line series to the chart
        pie.Series2.Add(lineSeries);
        pie.axes = XAxes;
    }

    public static void setChartTitle(string s, Pie pie)
    {
        pie.TitleChart.Text = s;
    }
}
