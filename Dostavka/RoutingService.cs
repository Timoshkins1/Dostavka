using Dostavka.Models;
using System.Collections.Generic;
using System.Linq;

namespace Dostavka.Services
{
    public class RoutingService
    {
        public Point Depot { get; set; } = new Point { X = 55.75, Y = 37.61 };

        public double CalculateDistance(Point a, Point b)
        {
            return System.Math.Sqrt(System.Math.Pow(a.X - b.X, 2) + System.Math.Pow(a.Y - b.Y, 2));
        }

        public bool TestRoutingSolution(Point depot, Order[] orders, int[] route, out double totalCost)
        {
            totalCost = 0;
            if (route.Length < 2 || route[0] != -1 || route[^1] != -1)
                return false;

            Point current = depot;
            var visitedIds = new HashSet<int>();

            for (int i = 1; i < route.Length; i++)
            {
                Point next;
                if (route[i] == -1)
                {
                    next = depot;
                }
                else
                {
                    var order = orders.FirstOrDefault(o => o.ID == route[i]);
                    if (order == null || visitedIds.Contains(order.ID))
                        return false;

                    next = order.Destination;
                    visitedIds.Add(order.ID);
                }

                totalCost += CalculateDistance(current, next);
                current = next;
            }

            return visitedIds.Count == orders.Length;
        }

        public (int[] Route, double Cost) BuildRoute(List<Order> orders)
        {
            if (orders.Count == 0)
                return (null, 0);

            var unvisited = new List<Order>(orders);
            var route = new List<int> { -1 };
            Point current = Depot;

            while (unvisited.Count > 0)
            {
                var next = unvisited
                    .OrderBy(o => CalculateDistance(current, o.Destination) / (o.Priority + 0.01))
                    .First();

                route.Add(next.ID);
                current = next.Destination;
                unvisited.Remove(next);
            }

            route.Add(-1);

            if (TestRoutingSolution(Depot, orders.ToArray(), route.ToArray(), out double fullCost))
            {
                int lastOrderId = route[route.Count - 2];
                var lastOrderPoint = orders.First(o => o.ID == lastOrderId).Destination;
                double returnDistance = CalculateDistance(lastOrderPoint, Depot);
                double costWithoutReturn = fullCost - returnDistance;

                return (route.ToArray(), costWithoutReturn);
            }

            return (null, 0);
        }
    }
}