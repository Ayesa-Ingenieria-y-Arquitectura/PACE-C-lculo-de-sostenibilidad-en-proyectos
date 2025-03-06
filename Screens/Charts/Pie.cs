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

namespace Bc3_WPF.Screens.Charts;

public class Pie
{
    public IEnumerable<ISeries> Series { get; set; }

    public ObservableCollection<ISeries> Series2 { get; set; }

    public LabelVisual TitleChart { get; set; }

    public LabelVisual TitlePie { get; set; }

    public Pie()
    {
        Series2 = new ObservableCollection<ISeries>
        {
            new LineSeries<int>
            {
                Values = new ObservableCollection<int> {}, // Ensure it's ObservableCollection<int>
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
            Text = "Número de hijos",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15)
        };

        TitlePie = new LabelVisual
        {
            Text = "Cantidad por hijos",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15)
        };
    }

    public static void setDoughtData(List<KeyValuePair<string, float?>> data, Pie pie)
    {
        List<float?> values = data.Select(e => e.Value).ToList();
        List<string> names = data.Select(e => e.Key).ToList();
        int _index = 0;

        pie.Series = values.AsPieSeries((value, series) =>
        {
            series.Name = names[_index++ % names.Count];
            series.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Start;
            series.DataLabelsSize = 15;
        });
    }

    public static void updateLineChart(int data, Pie pie)
    {
        if (pie.Series2[0] is LineSeries<int> lineSeries && lineSeries.Values is ObservableCollection<int> values)
        {
            values.Add(data);

            pie.Series2[0] = new LineSeries<int>
            {
                Values = values, // Ensure it's ObservableCollection<int>
                Fill = null,
                GeometrySize = 20,
            };
        }
    }
}
