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
        private readonly Person _user;
        private readonly IPersonRepository _repository;
        private readonly IBbqRepository _bbqs;
        private readonly SnapshotStore _snapshots;

        public RunGetInvites(Person user, IPersonRepository repository, IBbqRepository bbqs, SnapshotStore snapshots)
        {
            _user = user;
            _repository = repository;
            _bbqs = bbqs;
            _snapshots = snapshots;
        }

        [Function(nameof(RunGetInvites))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/invites")] HttpRequestData req)
        {

            List<object> NewInvitesList = new();

            try
            {
                var InvitesList = await _repository.GetAsync(_user.Id);

                if (InvitesList != null)
                {
                    foreach (var bbqId in InvitesList.Invites.Where(i => i.InviteDate > DateTime.Now).Select(o => o.Id).ToList())
                    {
                        var bbqEvent = await _bbqs.GetAsync(bbqId);
                        if (bbqEvent != null)
                            return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded");
                        else
                           // _snapshots AsQueryable<Bbq>("Bbq").ToListAsync();
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
