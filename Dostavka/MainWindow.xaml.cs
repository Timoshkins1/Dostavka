using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BestDelivery;

namespace Dostavka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Order> orders = new();
        BestDelivery.Point depot = new() { X = 55.75, Y = 37.61 };
        Random rand = new();
        double minX, maxX, minY, maxY;

        public MainWindow()
        {
            InitializeComponent();
            OrderSetSelector.SelectedIndex = 0;
        }
        private int[] lastRoute = null;
        private double lastRouteCost = 0;

        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateBounds();
            DrawMap(orders); // Теперь рисует депо даже при orders.Count == 0

            if (lastRoute != null)
            {
                DrawRoute(lastRoute);
                ShowRouteInfo(lastRoute, lastRouteCost);
            }
        }

        private void OrderSetSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (ComboBoxItem)OrderSetSelector.SelectedItem;
            if (selected?.Tag == null) return;

            int setId = int.Parse(selected.Tag.ToString());
            orders.Clear();

            orders.AddRange(setId switch
            {
                1 => OrderArrays.GetOrderArray1(),
                2 => OrderArrays.GetOrderArray2(),
                3 => OrderArrays.GetOrderArray3(),
                4 => OrderArrays.GetOrderArray4(),
                5 => OrderArrays.GetOrderArray5(),
                6 => OrderArrays.GetOrderArray6(),
                _ => Array.Empty<Order>()
            });

            // удалим потенциальный мусор
            orders = orders.Where(o => o.ID != -1).ToList();

            CalculateBounds();
            DrawMap(orders);
            RefreshOrderList();
        }

        private void CalculateBounds()
        {
            var allPoints = new List<BestDelivery.Point> { depot };

            if (orders.Count > 0)
                allPoints.AddRange(orders.Select(o => o.Destination));

            minX = allPoints.Min(p => p.X);
            maxX = allPoints.Max(p => p.X);
            minY = allPoints.Min(p => p.Y);
            maxY = allPoints.Max(p => p.Y);

            // Добавляем защиту от одинаковых координат
            if (minX == maxX)
            {
                minX -= 0.0001;
                maxX += 0.0001;
            }
            if (minY == maxY)
            {
                minY -= 0.0001;
                maxY += 0.0001;
            }
        }

        private System.Windows.Point Normalize(BestDelivery.Point geo)
        {
            double scaleX = (geo.X - minX) / (maxX - minX + 0.0001);
            double scaleY = (geo.Y - minY) / (maxY - minY + 0.0001);
            return new System.Windows.Point(scaleX * MapCanvas.ActualWidth, scaleY * MapCanvas.ActualHeight);
        }

        private void DrawMap(IEnumerable<Order> visibleOrders, HashSet<int> highlightIds = null)
        {
            MapCanvas.Children.Clear();

            DrawPoint(depot, Brushes.DarkSlateBlue, 12, "Склад");

            foreach (var order in visibleOrders)
            {
                var fill = highlightIds?.Contains(order.ID) == true ? Brushes.ForestGreen : Brushes.OrangeRed;
                DrawPoint(order.Destination, fill, 10, $"#{order.ID}");
            }
        }
        private void RefreshOrderList()
        {
            OrderListBox.Items.Clear();

            foreach (var order in orders.OrderByDescending(o => o.Priority))
            {
                var item = new ListBoxItem
                {
                    Content = $"#{order.ID} | X:{order.Destination.X:F2} Y:{order.Destination.Y:F2} | P:{order.Priority:F2}",
                    Tag = order.ID
                };
                OrderListBox.Items.Add(item);
            }
        }
        private void ShowRouteInfo(int[] route, double cost)
        {
            RoutePanel.Children.Clear();

            var header = new TextBlock
            {
                Text = $"Стоимость маршрута: {cost:F2}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            RoutePanel.Children.Add(header);

            for (int i = 0; i < route.Length; i++)
            {
                string text;
                if (route[i] == -1)
                {
                    text = "🏢 Склад";
                }
                else
                {
                    // Находим заказ по ID и получаем его приоритет
                    var order = orders.FirstOrDefault(o => o.ID == route[i]);
                    string priorityText = order.ID != null ? order.Priority.ToString("0.##") : "?";
                    text = $"📦 Заказ #{route[i]} [Приоритет: {priorityText}]";
                }

                var step = new TextBlock
                {
                    Text = $"{i + 1}. {text}",
                    FontSize = 12
                };
                RoutePanel.Children.Add(step);
            }
        }

        private void DrawArrow(BestDelivery.Point from, BestDelivery.Point to)
        {
            var p1 = Normalize(from);
            var p2 = Normalize(to);

            // Линия маршрута
            var line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 2
            };
            MapCanvas.Children.Add(line);

            // Стрелка на конце
            Vector v = p1 - p2;
            v.Normalize();
            Vector ortho = new Vector(-v.Y, v.X);

            System.Windows.Point arrow1 = p2 + v * 10 + ortho * 5;
            System.Windows.Point arrow2 = p2 + v * 10 - ortho * 5;

            var triangle = new Polygon
            {
                Points = new PointCollection { p2, arrow1, arrow2 },
                Fill = Brushes.SteelBlue
            };
            MapCanvas.Children.Add(triangle);
        }


        private void DrawPoint(BestDelivery.Point pt, Brush fillColor, double size, string label = "")
        {
            var pos = Normalize(pt);

            // Кружок
            Ellipse circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = fillColor,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 0.5
                },
                ToolTip = label
            };

            Canvas.SetLeft(circle, pos.X - size / 2);
            Canvas.SetTop(circle, pos.Y - size / 2);
            MapCanvas.Children.Add(circle);

            // Подпись
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
                MapCanvas.Children.Add(text);
            }
        }



        private void DrawRoute(int[] route)
        {
            if (route.Length < 2) return;

            var visitedIds = new HashSet<int>(route.Where(id => id != -1));
            DrawMap(orders.Where(o => o.ID != -1), visitedIds);

            BestDelivery.Point last = depot;

            foreach (int id in route.Skip(1))
            {
                BestDelivery.Point next = (id == -1) ? depot : orders.First(o => o.ID == id).Destination;
                DrawArrow(last, next);
                last = next;
            }
        }
        private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем координаты клика относительно Canvas
            System.Windows.Point clickPoint = e.GetPosition(MapCanvas);

            // Преобразуем координаты Canvas в географические
            BestDelivery.Point geoPoint = new BestDelivery.Point
            {
                X = minX + (clickPoint.X / MapCanvas.ActualWidth) * (maxX - minX),
                Y = minY + (clickPoint.Y / MapCanvas.ActualHeight) * (maxY - minY)
            };

            // Генерируем уникальный ID
            int newId = 1;
            while (orders.Any(o => o.ID == newId) || newId == -1)
                newId++;

            // Создаем и добавляем заказ
            orders.Add(new Order
            {
                ID = newId,
                Destination = geoPoint,
                Priority = PrioritySlider.Value
            });

            // Сбрасываем последний маршрут
            lastRoute = null;
            RoutePanel.Children.Clear();

            // Обновляем интерфейс
            CalculateBounds();
            DrawMap(orders);
            RefreshOrderList();
        }
        private void BuildRoute_Click(object sender, RoutedEventArgs e)
        {
            if (orders.Count == 0)
            {
                MessageBox.Show("Нет заказов для построения маршрута.");
                return;
            }

            var unvisited = new List<Order>(orders);
            var route = new List<int> { -1 }; // начальная точка - склад

            BestDelivery.Point current = depot;

            while (unvisited.Count > 0)
            {
                var next = unvisited
                    .OrderBy(o =>
                        RoutingTestLogic.CalculateDistance(current, o.Destination) / (o.Priority + 0.01))
                    .First();

                route.Add(next.ID);
                current = next.Destination;
                unvisited.Remove(next);
            }

            // Добавляем возврат на склад в маршрут
            route.Add(-1);

            // Рассчитываем полную стоимость маршрута (включая возврат на склад)
            if (RoutingTestLogic.TestRoutingSolution(depot, orders.ToArray(), route.ToArray(), out double fullCost))
            {
                // Находим последний заказ в маршруте (предпоследний элемент)
                int lastOrderId = route[route.Count - 2];

                // Получаем точку последнего заказа
                var lastOrderPoint = orders.First(o => o.ID == lastOrderId).Destination;

                // Вычисляем расстояние от последнего заказа до склада
                double returnDistance = RoutingTestLogic.CalculateDistance(lastOrderPoint, depot);

                // Вычитаем это расстояние из полной стоимости
                double costWithoutReturn = fullCost - returnDistance;

                lastRoute = route.ToArray();
                lastRouteCost = costWithoutReturn; // сохраняем стоимость без возврата

                DrawRoute(lastRoute);
                ShowRouteInfo(lastRoute, lastRouteCost);
            }
            else
            {
                MessageBox.Show("Ошибка: построенный маршрут невалиден.");
            }
        }




        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            int newId = 1;
            while (orders.Any(o => o.ID == newId) || newId == -1)
            {
                newId++;
            }

            orders.Add(new Order
            {
                ID = newId,
                Destination = new BestDelivery.Point
                {
                    X = minX + rand.NextDouble() * (maxX - minX),
                    Y = minY + rand.NextDouble() * (maxY - minY)
                },
                Priority = PrioritySlider.Value
            });

            CalculateBounds();
            DrawMap(orders);
            RefreshOrderList();
        }

        private void RemoveSelectedOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrderListBox.SelectedItem is ListBoxItem selected)
            {
                int idToRemove = (int)selected.Tag;
                int index = orders.FindIndex(o => o.ID == idToRemove);
                if (index >= 0)
                {
                    orders.RemoveAt(index);
                    CalculateBounds();
                    DrawMap(orders);
                    RefreshOrderList();
                }
                else
                {
                    MessageBox.Show("Заказ не найден.");
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для удаления.");
            }
        }



        private void RemoveOrder_Click(object sender, RoutedEventArgs e)
        {
            if (orders.Count > 0)
            {
                orders.RemoveAt(orders.Count - 1);
                CalculateBounds();
                DrawMap(orders);
                RefreshOrderList();
            }
        }
    }
}