using BeatSpy.API.BeatSaver.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BeatSpy.API.BeatSaver
{
    public class BeatSaverApi
    {
        public int apiCallCount;
        private static string _baseURL = "https://beatsaver.com/api/";

        public BeatSaverApi()
        {
 
        }

        public static async Task<BeatSaverMapInfoModel> GetMapByHash(string hash)
        {
            var mapJsonDataBeatSaver = await Get($"maps/by-hash/{hash}");
            if (mapJsonDataBeatSaver == null) return null;
            var mapInfoBeatSaver = JsonConvert.DeserializeObject<BeatSaverMapInfoModel>(mapJsonDataBeatSaver);
            return mapInfoBeatSaver;
        }

        public static async Task<BeatSaverMapInfoModel> GetMapByKey(string key)
        {
            var mapJsonDataBeatSaver = await Get($"maps/detail/{key}");
            if (mapJsonDataBeatSaver == null) return null;
            var mapInfoBeatSaver = JsonConvert.DeserializeObject<BeatSaverMapInfoModel>(mapJsonDataBeatSaver);
            return mapInfoBeatSaver;
        }

        private static async Task<string> Get(string endpoint)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_baseURL + endpoint),
                    Method = HttpMethod.Get,
                };

                var productValue = new ProductInfoHeaderValue("ScraperBot", "1.0");
                var commentValue = new ProductInfoHeaderValue("(+http://www.example.com/ScraperBot.html)");

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.UserAgent.Add(productValue);
                client.DefaultRequestHeaders.UserAgent.Add(commentValue);

                var httpResponseMessage = await client.SendAsync(request);

                if (httpResponseMessage.StatusCode != HttpStatusCode.OK) return null;

                var data = await httpResponseMessage.Content.ReadAsStringAsync();
                return data;
            }
        }
    }
}
