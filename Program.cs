using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altra
{
  class Program
  {
    // Short-term trend: 10-20 days
    // Mid-term trend: 50 days
    // Long-term trend: 200 days
    private const string _apiKey = "C7PW7X54ISBEKC4N";
    private const string _symbol = "IBM";
    private const string _dailyAdjustedFuntionName = "TIME_SERIES_DAILY_ADJUSTED";
    private const string _globalQuoteFuntionName = "GLOBAL_QUOTE";
    private const string _smaFuntionName = "SMA";
    private const string _timePeriod = "10";
    private const string _interval = "daily";
    private const string _seriesType = "close";
    private readonly static HttpClient _client = new HttpClient();

    static async Task Main(string[] args)
    {
      // https://www.alphavantage.co/documentation/#dailyadj
      var dailyAdjustedUrl = $"https://www.alphavantage.co/query?function={_dailyAdjustedFuntionName}&symbol={_symbol}&apikey={_apiKey}";
      // https://www.alphavantage.co/documentation/#latestprice
      var quoteUrl = $"https://www.alphavantage.co/query?function={_globalQuoteFuntionName}&symbol={_symbol}&apikey={_apiKey}";
      // https://www.alphavantage.co/documentation/#sma
      var smaUrl = $"https://www.alphavantage.co/query?function={_smaFuntionName}&symbol={_symbol}&interval={_interval}&time_period={_timePeriod}&series_type={_seriesType}&apikey={_apiKey}";

      try
      {
        // Moving Average
        // 1. Identify Trends (up, down)
        // if the price is > MA --> uptrend
        // if the price is < MA --> downtrend

        // 2. Confirm Reversals
        // if the price is = MA --> trend reversal

        // 3. Identify Support and Resistance Level
        // TODO

        // 4. BUY/SELL
        // if the price is > MA --> signal to BUY with stop loss below the MA
        // if the price is < MA --> signal to SELL with stop loss above the MA


        // TYPES
        // 1. SMA - Simple Moving Average
        // Summary: Equal weight to all periods - laggy price reaction
        // Good for: Mid- to Long-term trends
        // Calculation: Sum of closing price for given period (days) / period length (number of days)
        // Example: 10 closing prices for each day in 10 days (c1+c2+...c10)/10

        // 2. WMA - Weighted Moving Average 
        // Summary: Responds faster to price action, more weight to recent periods, less weight to older periods, reflect a quicker shift in sentiment 
        // Good for: Short-term trends
        // Calculation: TODO

        // 3. EMA - Exponential Moving Average 
        // Summary: Responds faster to price action, more weight to recent periods, less weight to older periods, reflect a quicker shift in sentiment 
        // Good for: Short-term trends
        // Calculation: TODO

        // MA Crossovers
        // Summary: Combination of MA's reflecting two different time periods (short-term with long-term) create opportunity to trade MA Crossovers - used to determine if the trend has changed direction
        // Example 1: When a shorter period MA crosses above a longer period MA - signal uptrend --> signal to buy (Bullish).
        // Example 2: When a shorter period MA crosses below a longer period MA - signal downtrend --> signal to sell (Bearish).





        // Get todays date (Eastern)
        TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var todaysDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone).Date.ToString("yyyy-MM-dd");

        // Get current price
        string quoteResponseBody = await _client.GetStringAsync(quoteUrl);
        // var quoteRaw = JObject.Parse(quoteResponseBody);
        // var quote = quoteRaw["Global Quote"].ToObject<Quote>();
        var quote = ParseResponse<Quote>(quoteResponseBody, "Global Quote");
        var currentPrice = quote.Price;

        // Get current SMA
        string smaResponseBody = await _client.GetStringAsync(smaUrl);
        var smaRaw = JObject.Parse(smaResponseBody);
        var smaMetaData = smaRaw["Meta Data"].ToObject<SmaMetaData>();
        var sma = smaRaw["Technical Analysis: SMA"][todaysDate]["SMA"].ToObject<string>();
        //var sma = decimal.Parse(smaRaw[todaysDate].ToObject<string>());
        Console.WriteLine("Alpha SMA(" + _timePeriod + "): " + sma);

        HttpResponseMessage response = await _client.GetAsync(dailyAdjustedUrl);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        // Above three lines can be replaced with new helper method below
        // string responseBody = await client.GetStringAsync(uri);

        var daily = JObject.Parse(responseBody);
        var metaData = daily["Meta Data"].ToObject<InstrumentMetaData>();
        var timeSeriesRaw = daily["Time Series (Daily)"];
        var timeSeriesChildren = timeSeriesRaw.Children();
        var instruments = timeSeriesChildren.Select(ins => ins.First().ToObject<Instrument>());
        var term = 10;

        //CWObject(metaData);
        Console.WriteLine("SMA(" + term + "): " + CalculateSMA(term, instruments.Take(term)));
      }
      catch (HttpRequestException e)
      {
        Console.WriteLine("\nException Caught!");
        Console.WriteLine("Message :{0} ", e.Message);
      }
    }

    private static T ParseResponse<T>(string responseBody, string token)
    {
      var quoteRaw = JObject.Parse(responseBody);
      return quoteRaw[token].ToObject<T>();
    }
    private static void CWObject(object obj)
    {
      var type = obj.GetType();
      var properties = type.GetProperties();
      foreach (var p in properties)
      {
        Console.WriteLine(p.Name + ": " + p.GetValue(obj));
      }
    }


    private static decimal CalculateSMA(int term, IEnumerable<Instrument> instruments) => instruments.Sum(i => decimal.Parse(i.Close)) / term;

    internal class Instrument
    {
      [JsonProperty("1. open")]
      public string Open { get; set; }
      [JsonProperty("2. high")]
      public string High { get; set; }
      [JsonProperty("3. low")]
      public string Low { get; set; }
      [JsonProperty("4. close")]
      public string Close { get; set; }
      [JsonProperty("5. adjusted close")]
      public string AdjustedClose { get; set; }
      [JsonProperty("6. volume")]
      public string Volume { get; set; }
      [JsonProperty("7. dividend amount")]
      public string DividendAmount { get; set; }
      [JsonProperty("8. split coefficient")]
      public string SplitCoeff { get; set; }
    }

    internal class InstrumentMetaData
    {
      [JsonProperty("1. Information")]
      public string Information { get; set; }
      [JsonProperty("2. Symbol")]
      public string Symbol { get; set; }
      [JsonProperty("3. Last Refreshed")]
      public string LastRefreshed { get; set; }
      [JsonProperty("4. Output Size")]
      public string OutputSize { get; set; }
      [JsonProperty("5. Time Zone")]
      public string TimeZone { get; set; }
    }

    internal class SmaMetaData
    {
      [JsonProperty("1: Symbol")]
      public string Symbol { get; set; }
      [JsonProperty("2: Indicator")]
      public string Indicator { get; set; }

      [JsonProperty("3: Last Refreshed")]
      public string LastRefreshed { get; set; }
      [JsonProperty("4: Interval")]
      public string Interval { get; set; }
      [JsonProperty("5: Time Period")]
      public string TimePeriod { get; set; }
      [JsonProperty("6: Series Type")]
      public string SeriesType { get; set; }
      [JsonProperty("7: Time Zone")]
      public string TimeZone { get; set; }
    }

    internal class Quote
    {
      [JsonProperty("01. symbol")]
      public string Symbol { get; set; }
      [JsonProperty("02. open")]
      public string Open { get; set; }
      [JsonProperty("03. high")]
      public string High { get; set; }
      [JsonProperty("04. low")]
      public string Low { get; set; }
      [JsonProperty("05. price")]
      public string Price { get; set; }
      [JsonProperty("06. volume")]
      public string Volume { get; set; }
      [JsonProperty("07. latest trading day")]
      public string LatestTradingDay { get; set; }
      [JsonProperty("08. previous close")]
      public string PreviousClose { get; set; }
      [JsonProperty("09. change")]
      public string Change { get; set; }
      [JsonProperty("10. change percent")]
      public string ChangePct { get; set; }
    }
  }
}