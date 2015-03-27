﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Popups;
using MoneyManager.Foundation;
using MoneyManager.Foundation.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin;

namespace MoneyManager.Business.Manager {
    public class CurrencyManager {
        private const string CURRENCY_SERVICE_URL = "http://www.freecurrencyconverterapi.com/api/convert?q={0}&compact=y";
        private const string COUNTRIES_SERVICE_URL = "http://www.freecurrencyconverterapi.com/api/v2/countries";

        private static HttpClient _httpClient = new HttpClient();

        public async Task<List<Country>> GetSupportedCountries() {
            try {
                string jsonString = await GetJsonFromService(COUNTRIES_SERVICE_URL);

                var json = JsonConvert.DeserializeObject(jsonString) as JContainer;

                return (from JProperty token in json.Children().Children().Children()
                    select new Country {
                        Abbreviation = token.Name,
                        CurrencyID = token.Value["currencyId"].ToString(),
                        CurrencyName = token.Value["currencyName"].ToString(),
                        Name = token.Value["name"].ToString(),
                        Alpha3 = token.Value["alpha3"].ToString(),
                        ID = token.Value["id"].ToString()
                    })
                    .OrderBy(x => x.ID)
                    .ToList();
            }
            catch (Exception ex) {
                var dialog = new MessageDialog(Translation.GetTranslation("CheckInternetConnectionMessage"),
                    Translation.GetTranslation("CheckInternetConnectionTitle"));
                dialog.Commands.Add(new UICommand(Translation.GetTranslation("YesLabel")));

                dialog.ShowAsync();
            }
            return new List<Country>();
        }

        public async Task<double> GetCurrencyRatio(string currencyFrom, string currencyTo) {
            string currencyFromTo = string.Format("{0}-{1}", currencyFrom.ToUpper(), currencyTo.ToUpper());
            string url = string.Format(CURRENCY_SERVICE_URL, currencyFromTo);

            string jsonString = await GetJsonFromService(url);
            jsonString = jsonString.Replace(currencyFromTo, "Conversion");

            return ParseToExchangeRate(jsonString);
        }

        private double ParseToExchangeRate(string jsonString) {
            try {
                var typeExample =
                    new {
                        Conversion = new {
                            val = ""
                        }
                    };

                var currency = JsonConvert.DeserializeAnonymousType(jsonString, typeExample);
                //use US culture info for parsing, since service uses us format
                return Double.Parse(currency.Conversion.val, new CultureInfo("en-us"));
            }
            catch (Exception ex) {
                Insights.Report(ex, ReportSeverity.Error);
            }
            return 1;
        }

        private async Task<string> GetJsonFromService(string url) {
            try {
                PrepareHttpClient();
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await _httpClient.SendAsync(req);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex) {
                Insights.Report(ex, ReportSeverity.Error);
            }
            return "1";
        }

        private void PrepareHttpClient() {
            _httpClient = new HttpClient {BaseAddress = new Uri("https://api.SmallInvoice.com/")};
            _httpClient.DefaultRequestHeaders.Add("user-agent",
                "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
        }
    }
}