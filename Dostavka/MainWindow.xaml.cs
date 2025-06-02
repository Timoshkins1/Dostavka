using BestDelivery;
using Dostavka.Models;
using Dostavka.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Order = Dostavka.Models.Order; // Явное указание, какой Order использовать
using Point = Dostavka.Models.Point; // Явное указание, какой Point использовать

namespace Dostavka
{
    public partial class MainWindow : Window
    {
        private readonly RoutingService _routingService = new RoutingService();
        private readonly MapVisualizer _mapVisualizer;
        private List<Order> _orders = new List<Order>();
        private int[] _lastRoute = null;
        private double _lastRouteCost = 0;

        public MainWindow()
        {
            InitializeComponent();
            _mapVisualizer = new MapVisualizer(MapCanvas);
            OrderSetSelector.SelectedIndex = 0;
        }

        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawMap();
        }

        private void OrderSetSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (ComboBoxItem)OrderSetSelector.SelectedItem;
            if (selected?.Tag == null) return;

            int setId = int.Parse(selected.Tag.ToString());
            _orders.Clear();

            // Явное преобразование в List<Dostavka.Models.Order>
            _orders.AddRange((setId switch
            {
                1 => OrderArrays.GetOrderArray1().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                2 => OrderArrays.GetOrderArray2().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                3 => OrderArrays.GetOrderArray3().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                4 => OrderArrays.GetOrderArray4().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                5 => OrderArrays.GetOrderArray5().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                6 => OrderArrays.GetOrderArray6().Select(o => new Order { ID = o.ID, Destination = new Point { X = o.Destination.X, Y = o.Destination.Y }, Priority = o.Priority }),
                _ => Array.Empty<Order>()
            }).ToList());

            _orders = _orders.Where(o => o.ID != -1).ToList();
            RedrawMap();
            RefreshOrderList();
        }

        private void BuildRoute_Click(object sender, RoutedEventArgs e)
        {
            if (_orders.Count == 0)
            {
                MessageBox.Show("Нет заказов для построения маршрута.");
                return;
            }

            var (route, cost) = _routingService.BuildRoute(_orders);
            if (route != null)
            {
                _lastRoute = route;
                _lastRouteCost = cost;
                _mapVisualizer.DrawRoute(_routingService.Depot, _orders, route);
                ShowRouteInfo(route, cost);
            }
            else
            {
                MessageBox.Show("Ошибка: построенный маршрут невалиден.");
            }
        }

        private void MapCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clickPoint = e.GetPosition(MapCanvas);
            var geoPoint = new Point
            {
                X = _mapVisualizer.MinX + (clickPoint.X / MapCanvas.ActualWidth) * (_mapVisualizer.MaxX - _mapVisualizer.MinX),
                Y = _mapVisualizer.MinY + (clickPoint.Y / MapCanvas.ActualHeight) * (_mapVisualizer.MaxY - _mapVisualizer.MinY)
            };

            int newId = 1;
            while (_orders.Any(o => o.ID == newId) || newId == -1)
                newId++;

            _orders.Add(new Order
            {
                ID = newId,
                Destination = geoPoint,
                Priority = PrioritySlider.Value
            });

            _lastRoute = null;
            RoutePanel.Children.Clear();
            RedrawMap();
            RefreshOrderList();
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            var rand = new Random();
            int newId = 1;
            while (_orders.Any(o => o.ID == newId) || newId == -1)
                newId++;

            _orders.Add(new Order
            {
                ID = newId,
                Destination = new Point
                {
                    X = _mapVisualizer.MinX + rand.NextDouble() * (_mapVisualizer.MaxX - _mapVisualizer.MinX),
                    Y = _mapVisualizer.MinY + rand.NextDouble() * (_mapVisualizer.MaxY - _mapVisualizer.MinY)
                },
                Priority = PrioritySlider.Value
            });

            RedrawMap();
            RefreshOrderList();
        }

        private void RemoveSelectedOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrderListBox.SelectedItem is ListBoxItem selected)
            {
                int idToRemove = (int)selected.Tag;
                _orders.RemoveAll(o => o.ID == idToRemove);
                RedrawMap();
                RefreshOrderList();
            }
            else
            {
                MessageBox.Show("Выберите заказ для удаления.");
            }
        }

        private void RedrawMap()
        {
            _mapVisualizer.CalculateBounds(_routingService.Depot, _orders);
            _mapVisualizer.DrawMap(_routingService.Depot, _orders);

            if (_lastRoute != null)
            {
                _mapVisualizer.DrawRoute(_routingService.Depot, _orders, _lastRoute);
                ShowRouteInfo(_lastRoute, _lastRouteCost);
            }
        }

        private void RefreshOrderList()
        {
            OrderListBox.Items.Clear();
            foreach (var order in _orders.OrderByDescending(o => o.Priority))
            {
                OrderListBox.Items.Add(new ListBoxItem
                {
                    Content = $"#{order.ID} | X:{order.Destination.X:F2} Y:{order.Destination.Y:F2} | P:{order.Priority:F2}",
                    Tag = order.ID
                });
            }
        }

        private void ShowRouteInfo(int[] route, double cost)
        {
            RoutePanel.Children.Clear();

            RoutePanel.Children.Add(new TextBlock
            {
                Text = $"Стоимость маршрута: {cost:F2}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            for (int i = 0; i < route.Length; i++)
            {
                string text = route[i] == -1 ? "🏢 Склад" :
                    $"📦 Заказ #{route[i]} [Приоритет: {_orders.First(o => o.ID == route[i]).Priority:0.##}]";

                RoutePanel.Children.Add(new TextBlock
                {
                    Text = $"{i + 1}. {text}",
                    FontSize = 12
                });
            }
        }
    }
}