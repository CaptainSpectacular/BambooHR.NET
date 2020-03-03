using System;
using System.Configuration;
using System.Net.Http;

namespace BambooConsumer.Services
{
    class BambooService
    {
        protected readonly string _baseUrl;
        protected readonly string _creds;
        protected HttpClient _client;

        public BambooService()
        {
            _client = new HttpClient();
            _baseUrl = $"{ConfigurationManager.AppSettings["BaseUrl"]}";
            _creds = Convert.ToBase64String(System.Text.ASCIIEncoding
                .ASCII.GetBytes(ConfigurationManager.AppSettings["ApiKey"]));
        }

        protected void SetClientHeaders()
        {
            _client.DefaultRequestHeaders
              .Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", _creds);
            _client.DefaultRequestHeaders
                .Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
