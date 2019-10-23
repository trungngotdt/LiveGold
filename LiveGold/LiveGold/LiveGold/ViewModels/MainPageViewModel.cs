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

namespace LiveGold.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        const string upColor = "#11ff00";
        const string downColor = "Red";
        public string ColorBuyLocal { get; set; }
        public string ColorBuyGlobal { get; set; }
        public string ColorSellLocal { get; set; }
        public string BuyGlobal { get; set; }
        public string SellGlobal { get; set; }
        public string BuyLocal { get; set; }
        public string SellLocal { get; set; }
        public bool IsLoading { get; set; }
        private readonly IPageDialogService dialogService;
        public MainPageViewModel(INavigationService navigationService, IPageDialogService _dialogService)
            : base(navigationService)
        {
            Title = "Main Page";
            dialogService = _dialogService;
            DefaultValue();
        }

        void DefaultValue()
        {
            ColorSellLocal = ColorBuyLocal = ColorBuyGlobal = upColor;
            BuyGlobal = BuyLocal = SellGlobal = SellLocal = "0";
        }

        async Task GetLocalPrice()
        {
            using (var client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,

            }))
            {
                string timespan = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                ;
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
                XmlNode node = doc.DocumentElement.SelectNodes("/root/ratelist/city/item")[0];
                var temp = node.Attributes["buy"].Value;
                var temp1 = node.Attributes["sell"].Value;
                ColorBuyLocal = float.Parse(temp) >= float.Parse(BuyLocal) ? upColor : downColor;
                ColorSellLocal = float.Parse(temp1) >= float.Parse(SellLocal) ? upColor : downColor;
                BuyLocal = temp;
                SellLocal = temp1;
                RaisePropertyChanged("BuyLocal");
                RaisePropertyChanged("SellLocal");

                RaisePropertyChanged("ColorBuyLocal");
                RaisePropertyChanged("ColorSellLocal");
                httpRequestMessage.Dispose();
                xml = null;
                doc = null;
                node = null;
                temp = null;
                temp1 = null;
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
                ColorBuyGlobal =float.Parse( temp )>= float.Parse(BuyGlobal) ? upColor : downColor;
                BuyGlobal = temp;
                RaisePropertyChanged("BuyGlobal");
                RaisePropertyChanged("ColorBuyGlobal");
                json = String.Empty;
                data = null;
                prices = null;
                temp = null;
                httpRequestMessage.Dispose();
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
                    await dialogService.DisplayAlertAsync("Error", "Something is wrong", "OK");
                }


            });
            base.Initialize(parameters);
        }


    }
}
