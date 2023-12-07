using Domain;
using Eveneum;
using System.Net;
using Domain.Events;
using Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public class Function
    {
        private readonly Person _user;
        private readonly IEventStore _eventStore;
        public Function(IEventStore eventStore, Person user)
        {
            _user = user;
            _eventStore = eventStore;
        }

        public class QueroMarcar
        {
            public string Motivo { get; set; }
            public bool BancadoPelosSocios { get; set; }
            public DateTime NoDia { get; set; }
        }

        [Function(nameof(RunChurrasQuando))]
        public async Task<HttpResponseData> RunChurrasQuando([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{id}")] HttpRequestData req, string id)
        {
            var stream = await _eventStore.ReadStream(id);

            return await req.CreateResponse(HttpStatusCode.OK, stream.Stream?.Snapshot?.Data);
        }

        [Function(nameof(RunReconstroiOChurras))]
        public async Task<HttpResponseData> RunReconstroiOChurras([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{id}/reconstroi")] HttpRequestData req, string id)
        {
            var stream = await _eventStore.ReadStream(id, new ReadStreamOptions { IgnoreSnapshots = true, FromVersion = 0, MaxItemCount = 100 });

            var events = stream.Stream?.Events;

            var churras = new Bbq();

            var loadedEvents = events?.Select(@event => (IEvent)@event.Body);

            churras.Rehydrate(loadedEvents);

            return await req.CreateResponse(HttpStatusCode.Created, churras.TakeSnapshot());

        }
    }
}
