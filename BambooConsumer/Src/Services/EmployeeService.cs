using Newtonsoft.Json;
using BambooConsumer.Models;
using System.Net.Http;

namespace BambooConsumer.Services
{
    class EmployeeService : BambooService
    {
        private readonly string _parameters;

        public EmployeeService()
        {
            _parameters = "?fields=firstName,lastName,department,customLoginName,displayName,jobTitle,state,status,supervisor,workPhone,mobilePhone,workEmail";
            SetClientHeaders();
        }

        public BambooEmployee GetEmployee(string id)
        {
            HttpResponseMessage response =  _client.GetAsync(_baseUrl + id + _parameters).Result;
            string rawJson = "";
            BambooEmployee employee = null;

            if (response.IsSuccessStatusCode)
            {
                rawJson = response.Content.ReadAsStringAsync().Result;
                employee = JsonConvert.DeserializeObject<BambooEmployee>(rawJson);
            }

            return employee;
        }
    }
}
