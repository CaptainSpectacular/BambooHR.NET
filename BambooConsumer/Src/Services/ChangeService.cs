using System;
using Newtonsoft.Json;
using BambooConsumer.Models;
using System.Net.Http;

namespace BambooConsumer.Services
{
    class ChangeService : BambooService
    {
        private readonly string _url;

        public ChangeService()
        {
            string lastHalfHour = DateTime
                .Now
                .AddMinutes(-30)
                .ToString("yyyy-MM-ddTHH:mm:00-06:00");
            _url = $"changed/?since={lastHalfHour}";
        }

        public ChangeContainer GetChangedEmployees()
        {
            SetClientHeaders();
            HttpResponseMessage response = _client.GetAsync(_baseUrl + _url).Result;
            string rawJson = "";
            ChangeContainer employeeList = null;

            if (response.IsSuccessStatusCode)
            {
                rawJson = response.Content.ReadAsStringAsync().Result;
                employeeList = JsonConvert.DeserializeObject<ChangeContainer>(rawJson);
            }

            return employeeList;
        }
    }
}
