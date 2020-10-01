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
    private const string _funtionName = "TIME_SERIES_DAILY_ADJUSTED";
    private readonly static HttpClient _client = new HttpClient();

    static async Task Main(string[] args)
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

      // https://www.alphavantage.co/documentation/#dailyadj
      var dailyAdjustedUrl = $"https://www.alphavantage.co/query?function={_funtionName}&symbol=IBM&apikey={_apiKey}";
      try
      {
        HttpResponseMessage response = await _client.GetAsync(dailyAdjustedUrl);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        // Above three lines can be replaced with new helper method below
        // string responseBody = await client.GetStringAsync(uri);

        var daily = JObject.Parse(responseBody);
        var metaData = daily["Meta Data"].ToObject<EquityMetaData>();
        var timeSeriesRaw = daily["Time Series (Daily)"];
        var timeSeriesChildren = timeSeriesRaw.Children();
        var equities = timeSeriesChildren.Select(eq => eq.First().ToObject<Equity>());

        CWObject(metaData);
        foreach (var equity in equities.Take(5))
        {
          CWObject(equity);
        }
      }
      catch (HttpRequestException e)
      {
        Console.WriteLine("\nException Caught!");
        Console.WriteLine("Message :{0} ", e.Message);
      }
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

    internal class Equity
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

    internal class EquityMetaData
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
  }
}