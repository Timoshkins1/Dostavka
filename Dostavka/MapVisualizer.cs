using Dostavka.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = Dostavka.Models.Point; // Явное указание, какой Point использовать

namespace Dostavka.Services
{
    public class MapVisualizer
    {
        private readonly Canvas _canvas;
        private double _minX, _maxX, _minY, _maxY;

        public double MinX => _minX;
        public double MaxX => _maxX;
        public double MinY => _minY;
        public double MaxY => _maxY;

        public MapVisualizer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void CalculateBounds(Point depot, IEnumerable<Order> orders)
        {
            var allPoints = new List<Point> { depot };

            if (orders.Any())
                allPoints.AddRange(orders.Select(o => o.Destination));

            _minX = allPoints.Min(p => p.X);
            _maxX = allPoints.Max(p => p.X);
            _minY = allPoints.Min(p => p.Y);
            _maxY = allPoints.Max(p => p.Y);

            if (_minX == _maxX)
            {
                _minX -= 0.0001;
                _maxX += 0.0001;
            }
            if (_minY == _maxY)
            {
                _minY -= 0.0001;
                _maxY += 0.0001;
            }
        }

        private System.Windows.Point Normalize(Point geo)
        {
            double scaleX = (geo.X - _minX) / (_maxX - _minX + 0.0001);
            double scaleY = (geo.Y - _minY) / (_maxY - _minY + 0.0001);
            return new System.Windows.Point(scaleX * _canvas.ActualWidth, scaleY * _canvas.ActualHeight);
        }

        public void DrawMap(Point depot, IEnumerable<Order> orders, HashSet<int> highlightIds = null)
        {
            _canvas.Children.Clear();
            DrawPoint(depot, Brushes.DarkSlateBlue, 12, "Склад");

            foreach (var order in orders)
            {
                var fill = highlightIds?.Contains(order.ID) == true ? Brushes.ForestGreen : Brushes.OrangeRed;
                DrawPoint(order.Destination, fill, 10, $"#{order.ID}");
            }
        }

        public void DrawRoute(Point depot, List<Order> orders, int[] route)
        {
            if (route == null || route.Length < 2) return;

            var visitedIds = new HashSet<int>(route.Where(id => id != -1));
            DrawMap(depot, orders.Where(o => o.ID != -1), visitedIds);

            Point last = depot;

            foreach (int id in route.Skip(1))
            {
                Point next = (id == -1) ? depot : orders.First(o => o.ID == id).Destination;
                DrawArrow(last, next);
                last = next;
            }
        }

        private void DrawPoint(Point pt, Brush fillColor, double size, string label = "")
        {
            var pos = Normalize(pt);

            var circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = fillColor,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                ToolTip = label
            };

            Canvas.SetLeft(circle, pos.X - size / 2);
            Canvas.SetTop(circle, pos.Y - size / 2);
            _canvas.Children.Add(circle);

            if (!string.IsNullOrEmpty(label))
            {
                var text = new TextBlock
                {
                    Text = label,
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, pos.X + size / 2 + 2);
                Canvas.SetTop(text, pos.Y - 5);
                _canvas.Children.Add(text);
            }
        }

        private void DrawArrow(Point from, Point to)
        {
            var p1 = Normalize(from);
            var p2 = Normalize(to);

            var line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 2
            };
            _canvas.Children.Add(line);

            System.Windows.Vector v = p1 - p2;
            v.Normalize();
            System.Windows.Vector ortho = new System.Windows.Vector(-v.Y, v.X);

            System.Windows.Point arrow1 = p2 + v * 10 + ortho * 5;
            System.Windows.Point arrow2 = p2 + v * 10 - ortho * 5;

            var triangle = new Polygon
            {
                Points = new PointCollection { p2, arrow1, arrow2 },
                Fill = Brushes.SteelBlue
            };
            _canvas.Children.Add(triangle);
        }
    }
}   