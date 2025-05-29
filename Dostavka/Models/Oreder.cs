using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dostavka.Models
{
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Order
    {
        public int ID { get; set; }
        public Point Destination { get; set; }
        public double Priority { get; set; }
    }
}
