using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.AppModel;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Xamarin.Essentials;
using System.Threading.Tasks;
using System.Xml;
using Prism.Services;
using LiveGold.Models;
using HtmlAgilityPack;

namespace LiveGold.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        const string upColor = "#11ff00";
        const string downColor = "Red";
        
        public string ColorBuyGlobal { get; set; }
        public string ColorSellGlobal { get; set; }
        public string BuyGlobal { get; set; }
        public string SellGlobal { get; set; }
       
        public bool IsLoading { get; set; }
        private readonly IPageDialogService dialogService;
        public string Unit { get; set; }
        public string Updated { get; set; }
        public List<GoldSJC> GoldSJCs { get; set; }
        public MainPageViewModel(INavigationService navigationService, IPageDialogService _dialogService)
            : base(navigationService)
        {
            Title = "Main Page";
            dialogService = _dialogService;
            DefaultValue();
        }

        void DefaultValue()
        {
            GoldSJCs = new List<GoldSJC>();
            BuyGlobal =  SellGlobal = "0";
        }

        async Task GetLocalPrice()
        {
            using (var client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,

            }))
            {
                string timespan = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                GoldSJCs = new List<GoldSJC>();
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("http://www.sjc.com.vn/xml/tygiavang.xml?t=" + timespan),

                };

                httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "*/*");
                httpRequestMessage.Headers.TryAddWithoutValidation("accept-encoding", "gzip, deflate");
                httpRequestMessage.Headers.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9");
                httpRequestMessage.Headers.TryAddWithoutValidation("dnt", "1");
                var result = await client.SendAsync(httpRequestMessage);

                var xml = await result.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                var nodeRoot = doc.DocumentElement.SelectNodes("/root/ratelist");
                Unit = String.Concat(" ", nodeRoot[0].Attributes["unit"].Value);
                Updated = nodeRoot[0].Attributes["updated"].Value;
                
                if (nodeRoot[0].HasChildNodes)
                {
                    var nodeCities = nodeRoot[0].ChildNodes;
                    var lengthNodeCities = nodeCities.Count;
                    for (int i = 0; i < lengthNodeCities; i++)
                    {
                        //For now
                        if (i == 1)
                        {
                            break;
                        }
                        if (nodeCities[i].HasChildNodes)
                        {
                            var nodeItems = nodeCities[i].ChildNodes;
                            var count = nodeItems.Count;
                            for (int j = 0; j < count; j++)
                            {
                                GoldSJCs.Add(new GoldSJC()
                                {
                                    Buy = String.Concat(nodeItems[j].Attributes["buy"].Value, Unit),
                                    Sell = String.Concat(nodeItems[j].Attributes["sell"].Value, Unit),
                                    City = nodeCities[i].Attributes["name"].Value,
                                    Type = nodeItems[j].Attributes["type"].Value
                                });
                            }
                        }
                    }
                }
                RaisePropertyChanged("GoldSJCs");               
                httpRequestMessage.Dispose();
            }
        }

        async Task GetGlobalPrice()
        {
            
            using (var client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,

            }))
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://data-asg.goldprice.org/dbXRates/USD"),

                };

                httpRequestMessage.Headers.TryAddWithoutValidation("accept", "application/json, text/javascript, */*; q=0.01");
                httpRequestMessage.Headers.TryAddWithoutValidation("accept-encoding", "gzip, deflate, br");
                httpRequestMessage.Headers.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9");
                httpRequestMessage.Headers.TryAddWithoutValidation("dnt", "1");
                var result = await client.SendAsync(httpRequestMessage);
                
                var json = await result.Content.ReadAsStringAsync();
                var data = (JObject)JsonConvert.DeserializeObject(json);

                var prices = (data["items"].Value<JArray>())[0].Value<JObject>();
                var temp = prices["xauPrice"].Value<string>();
                ColorBuyGlobal = float.Parse(temp) >= float.Parse(BuyGlobal) ? upColor : downColor;
                BuyGlobal = temp;
                RaisePropertyChanged("BuyGlobal");
                RaisePropertyChanged("ColorBuyGlobal");
                httpRequestMessage.Dispose();
            }
        }

        public async Task GetBidAndAskGold()
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };
            using (var client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.GetAsync("https://www.kitco.com/charts/livegold.html");
                var html = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                var bid = htmlDoc.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("sp-bid"));
                var ask = htmlDoc.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("sp-ask"));
                var bidValue = bid == null ? String.Empty : bid.FirstChild.InnerText;
                var askValue = ask == null ? String.Empty : ask.FirstChild.InnerText;
                ColorBuyGlobal = float.Parse(bidValue) >= float.Parse(BuyGlobal) ? upColor : downColor;
                BuyGlobal = bidValue;

                ColorSellGlobal = float.Parse(askValue) >= float.Parse(BuyGlobal) ? upColor : downColor;
                SellGlobal = askValue;
                
                RaisePropertyChanged("BuyGlobal");
                RaisePropertyChanged("ColorBuyGlobal");
                RaisePropertyChanged("SellGlobal");
                RaisePropertyChanged("ColorSellGlobal");
                handler.Dispose();
                client.Dispose();

            }
        }
        public override void Initialize(INavigationParameters parameters)
        {
            IsLoading = true;
            RaisePropertyChanged("IsLoading");

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                        {
                            await Task.WhenAll(new[] { GetLocalPrice(), GetGlobalPrice() });
                            //await GetLocalPrice();
                            if (IsLoading)
                            {
                                IsLoading = false;
                                RaisePropertyChanged("IsLoading");
                            }
                        }
                        else
                        {
                            await dialogService.DisplayAlertAsync("Alert", "Check network", "OK");
                            IsLoading = true;
                            RaisePropertyChanged("IsLoading");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await dialogService.DisplayAlertAsync("Error", "Something is wrong " + ex.Message, "OK");
                }


            });
            base.Initialize(parameters);
        }


    }
}
