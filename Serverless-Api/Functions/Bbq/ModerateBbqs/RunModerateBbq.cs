using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Serverless_Api
{
    public partial class RunModerateBbq
    {
        private readonly SnapshotStore _snapshots;
        private readonly IPersonRepository _persons;
        private readonly IBbqRepository _repository;

        public RunModerateBbq(IBbqRepository repository, SnapshotStore snapshots, IPersonRepository persons)
        {
            _persons = persons;
            _snapshots = snapshots;
            _repository = repository;
        }

        [Function(nameof(RunModerateBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "churras/{id}/moderar")] HttpRequestData req, string id)
        {
            try
            {
                var bbq = await _repository.GetAsync(id);
                var bbqWillHappen = await req.Body<ModerateBbqRequest>();

                if (bbq != null)
                {
                    bbq.Apply(new BbqStatusUpdated(bbqWillHappen.GonnaHappen, bbqWillHappen.TrincaWillPay));
                }
                
                var lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

                //------LookupsHasBeenCreated

                await _repository.SaveAsync(bbq);

                lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));

                if (bbqWillHappen.GonnaHappen && bbqWillHappen.TrincaWillPay)
                {
                    lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));
                    foreach (var personId in lookups.PeopleIds)
                    {
                        var person = await _persons.GetAsync(personId);
                        var @event = new PersonHasBeenInvitedToBbq(bbq.Id, bbq.BbqDate, bbq.Reason);
                        person.Apply(@event);
                        await _persons.SaveAsync(person);
                    }
                }
                else
                {
                    foreach (var personId in lookups.ModeratorIds)
                    {
                        var person = await _persons.GetAsync(personId);
                        person.Apply(new InviteWasDeclined { InviteId = id, PersonId = person.Id });
                        await _persons.SaveAsync(person);
                    }
                }

                return await req.CreateResponse(System.Net.HttpStatusCode.OK, bbq.TakeSnapshot());
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
