using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace EbayWorker.Models
{
    class StringToBoolConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return "ok".Equals(reader.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    class UrlShortner
    {
        const string LINK_SHORTNER_ENDPOINT = "https://api.shorte.st/v1/data/url";
        const string LINK_SHORTNER_TOKEN = "283b6d17cce97d4719d9edd4f9c15035";

        public UrlShortner(Uri url): this()
        {
            Url = url;
        }

        [JsonConstructor]
        public UrlShortner()
        {

        }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringToBoolConverter))]
        public bool IsShort { get; set; }

        [JsonProperty("shortenedUrl")]
        public Uri ShortUrl { get; set; }

        [JsonIgnore]
        public Uri Url { get; private set; }

        public bool Shorten()
        {
            var data = Encoding.UTF8.GetBytes(string.Format("urlToShorten={0}", Url.AbsoluteUri));

            var request = (HttpWebRequest)WebRequest.Create(LINK_SHORTNER_ENDPOINT);
            request.Method = WebRequestMethods.Http.Put;
            request.Headers.Add("public-api-token", LINK_SHORTNER_TOKEN);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            string responseXml;
            try
            {
                using (var writer = request.GetRequestStream())
                {
                    writer.Write(data, 0, data.Length);
                }

                
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseXml = reader.ReadToEnd();
                }
            }
            catch(WebException)
            {
                return false;
            }
            

            var result = JsonConvert.DeserializeObject<UrlShortner>(responseXml);
            IsShort = result.IsShort;
            ShortUrl = result.ShortUrl;
            return true;
        }
    }
}
