using System;
using System.Collections.Generic;

namespace SignalR.StockTick.Models
{
    public class Stock
    {
        public Stock()
        {
            TimeSeries = new List<TimeSerie>();
        }

        public string Symbol { get; set; }
        public string DisplayName { get; set; }
        public ICollection<TimeSerie> TimeSeries { get; set; }
    }

    public class TimeSerie
    {
        public DateTime TimeStamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
