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
        public IList<TimeSerie> TimeSeries { get; set; }
        public string Status
        {
            get
            {
                if (TimeSeries.Count <= 0) return PriceStatus.None.ToString();

                if (TimeSeries[0].Close > TimeSeries[1].Close)
                {
                    return PriceStatus.Up.ToString();
                }

                return TimeSeries[0].Close < TimeSeries[1].Close ? PriceStatus.Down.ToString() : PriceStatus.Equal.ToString();
            }
        }
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
