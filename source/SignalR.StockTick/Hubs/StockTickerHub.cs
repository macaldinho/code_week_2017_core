using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using SignalR.StockTick.Models;

namespace SignalR.StockTick.Hubs
{
    public class StockTickerHub : Hub
    {
        readonly StockTicker _stockTicker;

        public StockTickerHub(StockTicker stockTicker)
        {
            _stockTicker = stockTicker;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stockTicker.GetAllStocks();
        }
    }
}
