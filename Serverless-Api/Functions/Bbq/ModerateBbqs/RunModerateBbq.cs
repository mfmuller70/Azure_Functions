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
                var bbqWillHappen = await req.Body<ModerateBbqRequest>();
                var bbqEvent = await _repository.GetAsync(id);

                if (bbqEvent != null)
                    bbqEvent.Apply(new BbqStatusUpdated(bbqWillHappen.GonnaHappen, bbqWillHappen.TrincaWillPay));
                else
                    return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded");
                
                var lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

                // if (lookups.)
                //------LookupsHasBeenCreated

                await _repository.SaveAsync(bbqEvent);

                //lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));

                if (bbqWillHappen.GonnaHappen && bbqWillHappen.TrincaWillPay)
                {
                   // lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));
                    foreach (var personId in lookups.PeopleIds)
                    {
                        var personsToInvite = await _persons.GetAsync(personId);
                        if (personsToInvite != null)
                        {
                            var @event = new PersonHasBeenInvitedToBbq(bbqEvent.Id, bbqEvent.BbqDate, bbqEvent.Reason);
                            personsToInvite.Apply(@event);
                            await _persons.SaveAsync(personsToInvite);
                        }
                        else
                            return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded");

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

                return await req.CreateResponse(System.Net.HttpStatusCode.OK, bbqEvent.TakeSnapshot());
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
