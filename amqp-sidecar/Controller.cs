using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace amqp_sidecar.Controllers
{
    [Route("/")]
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly IConnection _brokerConnection;
        private readonly HttpClient _httpClient;

        public Controller(IConnection brokerConnection, HttpClient httpClient)
        {
            this._brokerConnection = brokerConnection;
            this._httpClient = httpClient;
        }

        [HttpPost("{exchange}/{routingKey}")]
        public ActionResult<string> EnqueueMessage([FromBody] object messageBody, [FromRoute] string exchange, [FromRoute] string routingKey)
        {
            Console.WriteLine($"Received message to enqueue (exchange={exchange}, routingKey={routingKey}): {messageBody}");

            if (messageBody == null) return BadRequest($"Unable to parse `{nameof(messageBody)}` from content");
            if (String.IsNullOrEmpty(exchange)) return BadRequest($"Could not find required `{nameof(exchange)}` URI fragment.");
            if (String.IsNullOrEmpty(routingKey)) return BadRequest($"Could not find required  `{nameof(routingKey)}` URI fragment.");

            // enqueue request as message
            using (var channel = this._brokerConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: exchange, type: "topic");

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody));
                channel.BasicPublish(exchange: exchange,
                                     routingKey: routingKey,
                                     basicProperties: null,
                                     body: body);
            }

            return Ok();
        }
    }
}
