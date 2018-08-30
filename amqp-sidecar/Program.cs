using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using amqp_sidecar.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace amqp_sidecar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigureBrokerMessageHandler();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        private static void ConfigureBrokerMessageHandler()
        {
            // mock config until I figure out dynamic configs
            BrokerConfig config = new BrokerConfig();

            const string brokerConfigFilePath = "config/broker.json";
            if (!File.Exists("config/broker.json"))
            {
                Console.WriteLine($"Broker config does not exist at path {brokerConfigFilePath}");
                return;
            }

            Console.WriteLine("Found config/broker.config");

            // read in broker.json as config
            try
            {
                string configFileContents = File.ReadAllText(brokerConfigFilePath);
                config = JsonConvert.DeserializeObject<BrokerConfig>(configFileContents);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to read configuration file: {e}");
                return;
            }

            // don't start the consumer if there are no rules
            if (!config.Rules.Any()) return;

            var httpClient = new HttpClient();
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            foreach (var rule in config.Rules)
            {
                channel.ExchangeDeclare(exchange: rule.Exchange, type: "topic");

                channel.QueueDeclare(queue: rule.Queue,
                                                 durable: true,
                                                 exclusive: false,
                                                 autoDelete: false);

                foreach (var routingKey in rule.RoutingKeys)
                {
                    channel.QueueBind(queue: rule.Queue,
                                  exchange: rule.Exchange,
                                  routingKey: routingKey);
                }

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Received message: {message}");

                    Task.Run(async () =>
                    {
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Post, rule.EndpointUri);
                            // use StringContent because object has already been serialized, 
                            // but still use application/json for Content-Type (StringContent uses text/plain by default)
                            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
                            Console.WriteLine($"Sending request: {request}");
                            var response = await httpClient.SendAsync(request);

                            Console.WriteLine($"Posted message to {rule.EndpointUri}. Response: {response}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception thrown: {e}");
                        }
                    });
                };

                channel.BasicConsume(queue: rule.Queue,
                                 autoAck: true,
                                 consumer: consumer);

                Console.WriteLine($"Monitoring queue: {rule.Queue}");
            }
        }
    }
}
