using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalR.StockTick.Constants;
using SignalR.StockTick.Hubs;
using SignalR.StockTick.Models;

namespace SignalR.StockTick
{
    public class StockTicker
    {
        readonly SemaphoreSlim _updateStockPricesStreamLock = new SemaphoreSlim(1, 1);
        readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(60000);
        readonly Timer _timer;
        volatile bool _updatingStockPrices;
        volatile bool _stocksLoaded;
        IDictionary<string, string> _stockNames;
        readonly ConcurrentDictionary<string, Stock> _stocks = new ConcurrentDictionary<string, Stock>();

        public StockTicker(IHubContext<StockTickerHub> clients)
        {
            Clients = clients;

            LoadDefaultStocks();

            _timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);

            Task.Factory.StartNew(() =>
            {
                UpdateStockPrices(null);
            });
        }

        void LoadDefaultStocks()
        {
            _stockNames = new Dictionary<string, string>
            {
                {"MSFT",  "Microsoft"},
                {"AAPL",  "Apple"},
                {"GOOG", "Google"},
                {"AMZN",  "Amazon"},
                {"BAC", "Bank of America"},
                {"IBM", "IBM"},
                {"TSLA",  "Tesla"},
                {"AXP","American Express"},
                {"GE", "General Electric"}
            };

            _stockNames.ToList().ForEach(stock =>
            {
                _stocks.TryAdd(stock.Key, new Stock { Symbol = stock.Key, DisplayName = stock.Value });
            });
        }

        IHubContext<StockTickerHub> Clients
        {
            get;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stocks.Values;
        }

        async void UpdateStockPrices(object state)
        {
            DateTime estTime = GetCurrentEstTime();
            DateTime closeTime = GetCloseEstTime();

            var minutes = (estTime - closeTime).TotalMinutes;

            if (minutes > 0 && _stocksLoaded)
            {
                await BroadcastStocks(_stocks.Values);
            }
            else
            {
                await StreamStocks();
            }
        }

        async Task StreamStocks()
        {
            await _updateStockPricesStreamLock.WaitAsync();

            try
            {
                if (!_updatingStockPrices)
                {
                    _updatingStockPrices = true;
                    //var timer = new Stopwatch();
                    //timer.Start();
                    foreach (var stockName in _stockNames)
                    {
                        try
                        {
                            using (var client = new System.Net.Http.HttpClient {BaseAddress = new Uri(HttpConstants.BaseUri)})
                            {
                                client.DefaultRequestHeaders.Accept.Clear();
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HttpConstants.JsonHeader));

                                var response = await client.GetAsync(string.Format(HttpConstants.Request, stockName.Key));

                                if (response.IsSuccessStatusCode)
                                {
                                    var cvsResult = await response.Content.ReadAsStringAsync();

                                    if (cvsResult.StartsWith("timestamp"))
                                    {
                                        var stock = new Stock {Symbol = stockName.Key, DisplayName = stockName.Value};
                                        var lines = cvsResult.Replace("\r", "").Split('\n').Select(l => l.Split(',')).Where(x => x.Length > 1).ToList();

                                        foreach (var line in lines.Skip(1))
                                        {
                                            stock.TimeSeries.Add(new TimeSerie
                                            {
                                                TimeStamp = DateTime.Parse(line[0]),
                                                Open = decimal.Parse(line[1]),
                                                High = decimal.Parse(line[2]),
                                                Low = decimal.Parse(line[3]),
                                                Close = decimal.Parse(line[4]),
                                                Volume = decimal.Parse(line[5])
                                            });
                                        }
                                        _stocks.TryUpdate(stockName.Key, stock, _stocks[stockName.Key]);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            
                        }
                        finally
                        {
                            await BroadcastStock(_stocks[stockName.Key]);
                        }
                    }
                    //timer.Stop();
                    _updatingStockPrices = false;
                }
            }
            finally
            {
                _stocksLoaded = true;
                _updateStockPricesStreamLock.Release();
            }
        }

        async Task BroadcastStocks(IEnumerable<Stock> stocks)
        {
            await Clients.Clients.All.InvokeAsync("broadStocks", stocks);
        }

        async Task BroadcastStock(Stock stock)
        {
            await Clients.Clients.All.InvokeAsync("broadStock", stock);
        }

        DateTime GetCurrentEstTime()
        {
            var timeUtc = DateTime.UtcNow;
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);

            return easternTime;
        }

        DateTime GetCloseEstTime()
        {
            var closeTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 16, 0, 0);
            return closeTime;
        }
    }
}