namespace SignalR.StockTick.Constants
{
    public static class HttpConstants
    {
        public const string BaseUri = "https://www.alphavantage.co/";
        public const string JsonHeader = "application/json";
        public const string Request = "query?function=TIME_SERIES_INTRADAY&symbol={0}&interval=1min&outputsize=compact&datatype=csv&apikey=LG3HM7PZJT6PI0L6";
    }
}
