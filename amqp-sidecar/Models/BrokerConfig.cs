using System.Collections.Generic;

namespace amqp_sidecar.Models
{
    public class BrokerConfig
    {
        public IEnumerable<Rule> Rules { get; set; }

        public BrokerConfig(){
            this.Rules = new List<Rule>();
        }
    }

    public class Rule
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public IEnumerable<string> RoutingKeys { get; set; }
        public string EndpointUri { get; set; }
    }
}