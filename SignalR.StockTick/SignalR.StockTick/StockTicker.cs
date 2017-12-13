using System;
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
        readonly ICollection<Stock> _stocks;
        readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(60000);
        readonly Timer _timer;
        volatile bool _updatingStockPrices;
        volatile bool _stocksLoaded;
        DateTime _lastCloseTime;

        public StockTicker(IHubContext<StockTickerHub> clients)
        {
            Clients = clients;

            _stocks = new List<Stock>
            {
                new Stock { Symbol = "MSFT", DisplayName = "Microsoft"},
                new Stock { Symbol = "AAPL", DisplayName = "Apple"},
                new Stock { Symbol = "GOOG", DisplayName ="Google"},
                new Stock { Symbol = "AMZN", DisplayName = "Amazon"},
                new Stock { Symbol = "BAC", DisplayName = "Bank of America"},
                new Stock { Symbol = "IBM", DisplayName = "IBM"},
                new Stock { Symbol = "TSLA", DisplayName = "Tesla"},
                new Stock { Symbol = "AXP",DisplayName = "American Express"},
                new Stock { Symbol = "GE", DisplayName = "General Electric"}
            };

            _timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);

            Task.Factory.StartNew(() =>
            {
                UpdateStockPrices(null);
            });
        }

        IHubContext<StockTickerHub> Clients
        {
            get;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stocksLoaded ? _stocks : null;
        }

        async void UpdateStockPrices(object state)
        {
            DateTime estTime = GetEstTime();

            var minutes = (estTime - _lastCloseTime).TotalMinutes;

            if (minutes > 0 && _stocksLoaded)
            {
                await BroadcastStocks(_stocks);
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
                    foreach (var stock in _stocks)
                    {
                        using (var client = new System.Net.Http.HttpClient { BaseAddress = new Uri(HttpConstants.BaseUri) })
                        {
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HttpConstants.JsonHeader));

                            var response = await client.GetAsync(string.Format(HttpConstants.Request, stock.Symbol));

                            if (response.IsSuccessStatusCode)
                            {
                                var cvsResult = await response.Content.ReadAsStringAsync();

                                if (cvsResult.StartsWith("timestamp"))
                                {
                                    var lines = cvsResult.Replace("\r", "").Split('\n').Select(l => l.Split(',')).Where(x => x.Length > 1).ToList();

                                    stock.TimeSeries.Clear();

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

                                    _lastCloseTime = _stocks.First().TimeSeries.First().TimeStamp;
                                }
                            }
                        }
                    }
                    //timer.Stop();
                    _updatingStockPrices = false;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _stocksLoaded = true;
                _updateStockPricesStreamLock.Release();
                await BroadcastStocks(_stocks);
            }
        }

        async Task BroadcastStocks(IEnumerable<Stock> stocks)
        {
            await Clients.Clients.All.InvokeAsync("broadStocks", stocks);
        }

        DateTime GetEstTime()
        {
            var timeUtc = DateTime.UtcNow;
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);

            return easternTime;
        }
    }
}