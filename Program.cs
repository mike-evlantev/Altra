using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
    private const string _symbol = "TSLA";
    private const string _dailyAdjustedFuntionName = "TIME_SERIES_DAILY_ADJUSTED";
    private const string _globalQuoteFuntionName = "GLOBAL_QUOTE";
    private const string _smaFuntionName = "SMA";
    private const int _timePeriod10 = 10;
    private const int _timePeriod20 = 20;
    private const int _timePeriod50 = 50;
    private const int _timePeriod200 = 200;
    private const string _interval = "daily";
    private const string _seriesType = "close";
    private static decimal _account = 2500;
    private static decimal _currentPrice = 0;
    private static decimal _sma50 = 0;
    private static decimal _sma200 = 0;
    private static TrendType _trend = TrendType.Unknown;
    private static Position _position;
    private static HttpClient _client = new HttpClient();

    static async Task Main(string[] args)
    {
      // TODO: while market open
      while (true)
      {
        // https://www.alphavantage.co/documentation/#latestprice
        var quoteUrl = $"https://www.alphavantage.co/query?function={_globalQuoteFuntionName}&symbol={_symbol}&apikey={_apiKey}";
        // https://www.alphavantage.co/documentation/#sma
        var sma50Url = $"https://www.alphavantage.co/query?function={_smaFuntionName}&symbol={_symbol}&interval={_interval}&time_period={_timePeriod50.ToString()}&series_type={_seriesType}&apikey={_apiKey}";
        var sma200Url = $"https://www.alphavantage.co/query?function={_smaFuntionName}&symbol={_symbol}&interval={_interval}&time_period={_timePeriod200.ToString()}&series_type={_seriesType}&apikey={_apiKey}";

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

        Console.WriteLine(DateTime.UtcNow);
        Console.WriteLine("Symbol: " + _symbol);
        // Get current price
        string quoteResponseBody = await _client.GetStringAsync(quoteUrl);
        var quote = ParseResponse<Quote>(quoteResponseBody, "Global Quote");
        _currentPrice = decimal.Parse(quote.Price);
        Console.WriteLine("Price: " + _currentPrice.ToString());

        // Get current SMA50 & current SMA200
        _sma50 = await GetSma(sma50Url, _timePeriod50);
        _sma200 = await GetSma(sma200Url, _timePeriod200);

        // Trend
        _trend = UpdateTrend();
        Console.WriteLine("Trend: " + _trend.ToString());

        // Buy/Sell Signal
        var actionSignal = GetActionSignal();
        Console.WriteLine("Signal: " + actionSignal.ToString());

        // Buy/Sell
        switch (actionSignal)
        {
          case ActionSignalType.Buy:
            Buy();
            break;
          case ActionSignalType.Sell:
            Sell();
            break;
          case ActionSignalType.DoNothing:
          case ActionSignalType.Unknown:
          default:
            break;
        }
        Console.WriteLine("***************************");
        Console.WriteLine("Position: " + string.Format("{0:C}", _position.Value));
        Console.WriteLine("Cash: " + string.Format("{0:C}", _account));
        Console.WriteLine("Total: " + string.Format("{0:C}", _position.Value + _account));
        Console.WriteLine("***************************");

        // Sleep for 5 min and repeat
        Thread.Sleep(60000);
      }
    }

    private static T ParseResponse<T>(string responseBody, string token)
    {
      var quoteRaw = JObject.Parse(responseBody);
      return quoteRaw[token].ToObject<T>();
    }
    private static void CWObject(object obj, int indent = 0)
    {
      var type = obj.GetType();
      var properties = type.GetProperties();
      foreach (var p in properties)
      {
        Console.WriteLine(Indent(indent) + p.Name + ": " + p.GetValue(obj));
      }
    }
    private static async Task<decimal> GetSma(string url, int timePeriod)
    {
      decimal smaResult = 0;
      try
      {
        HttpResponseMessage response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        // string responseBody = await _client.GetStringAsync(url);
        var smaRaw = JObject.Parse(responseBody);
        var smaMetaData = smaRaw["Meta Data"].ToObject<SmaMetaData>();
        var sma = smaRaw["Technical Analysis: SMA"][smaMetaData.LastRefreshed]["SMA"].ToObject<string>();
        Console.WriteLine("SMA(" + timePeriod.ToString() + "): " + sma);
        smaResult = decimal.Parse(sma);
      }
      catch (HttpRequestException e)
      {
        Console.WriteLine("\nException Caught!");
        Console.WriteLine("Message :{0} ", e.Message);
      }
      return smaResult;
    }
    private static TrendType UpdateTrend()
    {
      if ((_trend == TrendType.Unknown || _trend == TrendType.Down) && _sma50 > _sma200)
        _trend = TrendType.Up;
      else if ((_trend == TrendType.Unknown || _trend == TrendType.Up) && _sma50 < _sma200)
        _trend = TrendType.Down;
      // else
      //   _trend = TrendType.Unknown;
      return _trend;
    }
    private static ActionSignalType GetActionSignal()
    {
      // if currently holding a position do nothing
      if (_position != null)
        return ActionSignalType.DoNothing;

      switch (_trend)
      {
        case TrendType.Up:
          return ActionSignalType.Buy;
        case TrendType.Down:
          return ActionSignalType.Sell;
        case TrendType.Unknown:
        default:
          return ActionSignalType.DoNothing; ;
      }
    }
    private static void Buy()
    {
      if (_account <= 0)
      {
        Console.WriteLine("Cannot Buy! Insufficient funds.");
      }
      else
      {
        if (_position != null)
        {
          Console.WriteLine("Cannot Buy! Currently holding position:");
        }
        else
        {
          // How many shares can we buy?
          // Round down to the nearest int
          var numOfInstruments = Convert.ToInt32(Math.Floor(_account / _currentPrice));
          if (numOfInstruments < 1)
          {
            Console.WriteLine("Cannot Buy! Insufficient funds to buy a whole share.");
          }
          else
          {
            // Total value of position
            var positionValue = numOfInstruments * _currentPrice;
            // Debit account
            _account -= positionValue;

            _position = new Position()
            {
              Symbol = _symbol,
              Count = numOfInstruments,
              Value = positionValue,
              PurchasePrice = _currentPrice,
            };
            Console.WriteLine("Successfully purchased position:");
          }
        }
      }
      CWObject(_position, 2);
    }
    private static void Sell()
    {
      if (_position == null)
      {
        Console.WriteLine("Cannot Sell! No position to sell.");
      }
      else
      {
        // Total value of position
        var positionValue = _position.Count * _currentPrice;
        // Credit account
        _account += positionValue;

        _position = null;
        Console.WriteLine("Successfully sold position.");
      }
    }

    private static async Task Test()
    {
      // https://www.alphavantage.co/documentation/#dailyadj
      var dailyAdjustedUrl = $"https://www.alphavantage.co/query?function={_dailyAdjustedFuntionName}&symbol={_symbol}&outputsize=full&apikey={_apiKey}";
      // HttpResponseMessage response = await _client.GetAsync(dailyAdjustedUrl);
      // response.EnsureSuccessStatusCode();
      // string responseBody = await response.Content.ReadAsStringAsync();
      // Above three lines can be replaced with new helper method below
      string responseBody = await _client.GetStringAsync(dailyAdjustedUrl);

      var daily = JObject.Parse(responseBody);
      var metaData = daily["Meta Data"].ToObject<InstrumentMetaData>();
      var timeSeriesRaw = daily["Time Series (Daily)"];
      var timeSeriesChildren = timeSeriesRaw.Children();
      var instruments = timeSeriesChildren.Select(ins => ins.First().ToObject<Instrument>());

      var calculatedSma50 = CalculateSMA(_timePeriod50, instruments.Take(_timePeriod50));
      var calculatedSma200 = CalculateSMA(_timePeriod200, instruments.Take(_timePeriod200));
      //CWObject(metaData);
      Console.WriteLine("SMA(" + _timePeriod50 + "): " + calculatedSma50);
      Console.WriteLine("SMA(" + _timePeriod200 + "): " + calculatedSma200);
    }

    private static decimal CalculateSMA(int term, IEnumerable<Instrument> instruments) => instruments.Sum(i => decimal.Parse(i.Close)) / term;

    private static string Indent(int count) => "".PadLeft(count);

    #region Helper Classes
    internal enum TrendType
    {
      Unknown,
      Up,
      Down,
    }

    internal enum ActionSignalType
    {
      Unknown,
      Buy,
      Sell,
      DoNothing,
    }

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

    internal class Position
    {
      public string Symbol { get; set; }
      public int Count { get; set; }
      public decimal Value { get; set; }
      public decimal PurchasePrice { get; set; }
      public decimal SalePrice { get; set; }
    }
    #endregion
  }
}