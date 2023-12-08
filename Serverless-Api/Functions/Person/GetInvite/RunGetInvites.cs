using CrossCutting;
using Domain;
using Domain.Entities;
using Domain.Repositories;
using Eveneum;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Serverless_Api
{
    public partial class RunGetInvites
    {
        private readonly Person _person;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;
        private readonly SnapshotStore _snapshotStore;

        public RunGetInvites(Person user, 
                                IPersonRepository personRepository, 
                                IBbqRepository bbqRepository, 
                                SnapshotStore snapshotStore)
        {
            _person = user;
            _personRepository = personRepository;
            _bbqRepository = bbqRepository;
            _snapshotStore = snapshotStore;
        }

        [Function(nameof(RunGetInvites))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/invites")] HttpRequestData req)
        {

            List<object> NewInvitesList = new();

            try
            {
                var InvitesList = await _personRepository.GetAsync(_person.Id);

                if (InvitesList != null)
                {
                    foreach (var bbqId in InvitesList.Invites.Where(i => i.InviteDate > DateTime.Now).Select(o => o.Id).ToList())
                    {
                        var bbqEvent = await _bbqRepository.GetAsync(bbqId);
                        if (bbqEvent == null)
                            return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded");
                        else
                            NewInvitesList.Add(bbqEvent.TakeSnapshot());
                    }
                }
                else
                    return await req.CreateResponse(HttpStatusCode.NotFound, "Without invites until now.");

                return await req.CreateResponse(HttpStatusCode.OK, NewInvitesList);
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
