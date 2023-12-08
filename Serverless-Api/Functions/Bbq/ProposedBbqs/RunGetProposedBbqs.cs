using System.Net;
using CrossCutting;
using Domain.Entities;
using Domain.Repositories;
using Eveneum;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunGetProposedBbqs
    {
        private readonly Person _person;
        private readonly IBbqRepository _bbqRepository;
        private readonly IPersonRepository _personRepository;
        private readonly SnapshotStore _snapshotStore;

        public RunGetProposedBbqs(IPersonRepository bbqRepository, 
                                    IBbqRepository bbqs, 
                                    Person personRepository,
                                     SnapshotStore snapshotStore)
        {
            _person = personRepository;
            _bbqRepository = bbqs;
            _personRepository = bbqRepository;
            _snapshotStore = snapshotStore;
        }

        [Function(nameof(RunGetProposedBbqs))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras")] HttpRequestData req)
        {
            try
            {
                var lookups = await _snapshotStore.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

                List<object> ProposalList = new();
                var bbqEventInformation = await _personRepository.GetAsync(_person.Id);
                foreach (var bbqId in bbqEventInformation.Invites.Where(i => i.InviteDate > DateTime.Now).Select(o => o.Id).ToList())
                {
                    var bbq = await _bbqRepository.GetAsync(bbqId);
                    ProposalList.Add(bbq.TakeSnapshot());
                }

                return await req.CreateResponse(HttpStatusCode.Created, ProposalList);
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
