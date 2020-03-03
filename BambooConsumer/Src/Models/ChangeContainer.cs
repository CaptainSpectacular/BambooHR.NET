using Newtonsoft.Json;
using System.Collections.Generic;

namespace BambooConsumer.Models
{
    class ChangeContainer
    {
        [JsonProperty]
        public Dictionary<string, ChangeEmployee> Employees { get; set; }
    }
}
