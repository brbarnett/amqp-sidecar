using System.Net.Http;

using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Fakes;

using System.Net;

namespace amqp_sidecar_tests
{
    // reference: https://github.com/Parametric/RabbitMQ.Fakes/blob/master/RabbitMQ.Fakes.Tests/UseCases/SendMessages.cs
    [TestFixture]
    public class Publishing
    {
        private RabbitServer _rabbitServer;
        private FakeConnectionFactory _connectionfactory;

        [SetUp]
        public void Setup()
        {
            // rabbitmq
            this._rabbitServer = new RabbitServer();
            this._connectionfactory = new FakeConnectionFactory(this._rabbitServer);
        }

        [Test, Sequential]
        public void PublishNullMessageBody(
            [Values("", "payments", "", "payments")] string exchange,
            [Values("", "", "payments.create", "payments.create")] string routingKey)
        {
            // arrange
            var controller = new amqp_sidecar.Controllers.Controller(this._connectionfactory.CreateConnection(), new HttpClient());

            // act
            ActionResult<string> actionResult = controller.EnqueueMessage(null, exchange, routingKey);

            // assert
            var result = actionResult.Result as BadRequestObjectResult;
            Assert.That(result, Is.Not.Null, "Result is not BadRequestObjectResult");
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest), "Result should be status code 400");
            Assert.That(this._rabbitServer.Exchanges.ContainsKey(exchange), Is.EqualTo(false), "Exchange should not be created");
        }

        [Test, Sequential]
        public void PublishMessageBodyWithNullParmaters(
            [Values("", "payments", "")] string exchange,
            [Values("", "", "payments.create")] string routingKey)
        {
            // arrange
            var controller = new amqp_sidecar.Controllers.Controller(this._connectionfactory.CreateConnection(), new HttpClient());
            var messageBody = new SubmitPaymentRequest
            {
                AccountNumber = "12345",
                PaymentAmount = 100
            };

            // act
            ActionResult<string> actionResult = controller.EnqueueMessage(messageBody, exchange, routingKey);

            // assert
            var result = actionResult.Result as BadRequestObjectResult;
            Assert.That(result, Is.Not.Null, "Result is not BadRequestObjectResult");
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest), "Result should be status code 400");
            Assert.That(this._rabbitServer.Exchanges.ContainsKey(exchange), Is.EqualTo(false), "Exchange should not be created");
        }

        [Test]
        public void PublishMessageValid( 
            [Values("payments")] string exchange,
            [Values("payments.create")] string routingKey)
        {
            // arrange
            var controller = new amqp_sidecar.Controllers.Controller(this._connectionfactory.CreateConnection(), new HttpClient());
            var messageBody = new SubmitPaymentRequest
            {
                AccountNumber = "12345",
                PaymentAmount = 100
            };

            // act
            ActionResult<string> actionResult = controller.EnqueueMessage(messageBody, exchange, routingKey);

            // assert
            var result = actionResult.Result as OkResult;
            Assert.That(result, Is.Not.Null, "Result is not OkResult");
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK), "Result should be status code 200");
            Assert.That(this._rabbitServer.Exchanges.ContainsKey(exchange), Is.EqualTo(true), "Exchange should be created");
            Assert.That(this._rabbitServer.Exchanges[exchange].Messages.Count, Is.EqualTo(1), "Expected message in exchange");
        }

        private class SubmitPaymentRequest
        {
            public string AccountNumber { get; set; }

            public decimal PaymentAmount { get; set; }
        }
    }
}