using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Models
{
    public class Currencies
    {
        private DateTime LastUpdate;

        private HttpClient Client;

        private List<Currency_ExRate> CurrenciesList;

        public Currencies()
        {
            Client = new HttpClient();
            CurrenciesList = new List<Currency_ExRate>();

            Update();
        }

        public Currency_ExRate GetCurrencyByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            switch (name)
            {
                case "alt":
                    name = "alteration";
                    break;

                case "fuse":
                    name = "fusing";
                    break;
                case "exa":
                    name = "exalted";
                    break;
                case "alch":
                    name = "alchemy";
                    break;

            }

            return CurrenciesList.Find((Currency_ExRate c) => c.Name.Contains(name.ToLower()));
        }

        public void Update()
        {
            var response = Client.GetAsync("https://poe.ninja/api/Data/GetCurrencyOverview?league=Betrayal").Result;
            var responseBody = response.Content.ReadAsStringAsync().Result;

            var ExchangeRatesJson = JsonConvert.DeserializeObject<CurrenciesJson>(responseBody);

            CurrenciesList.Clear();

            foreach (Line l in ExchangeRatesJson.Lines)
            {
                Currency_ExRate c = new Currency_ExRate(l.CurrencyTypeName, l.ChaosEquivalent);

                CurrenciesList.Add(c);
            }
            CurrenciesList.Add(new Currency_ExRate("Chaos Orb", 1));

            //foreach (CurrencyDetail cd in ExchangeRatesJson.CurrencyDetails)
            //{
            //    var img = "Assets/Currencies/" + cd.Name.ToLower().Replace(" ", "") + ".png";

            //    if (!File.Exists(img))
            //    {
            //        using (WebClient client = new WebClient())
            //        {
            //            client.DownloadFile(cd.Icon, img);
            //        }
            //    }
            //}


            Console.WriteLine("Curencies updated!");
        }
    }

    public class Currency_ExRate
    {
        public string Name { get; set; }

        public string ImageName { get; set; }

        public double ChaosEquivalent { get; set; }

        public Currency_ExRate(string name, double chaosequivalent)
        {
            Name = name.ToLower();

            ChaosEquivalent = chaosequivalent;

            ImageName = "Assets/Currencies/" + Name.Replace(" ", "") + ".png";

        }

        public override string ToString()
        {
            return Name; 
        }
    }
}
