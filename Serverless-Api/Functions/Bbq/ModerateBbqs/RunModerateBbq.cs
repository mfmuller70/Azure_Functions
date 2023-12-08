using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;

namespace Serverless_Api
{
    public partial class RunModerateBbq
    {
        private readonly SnapshotStore _snapshotStore;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;

        public RunModerateBbq(IBbqRepository bbqRepository, 
                                SnapshotStore snapshotStore, 
                                IPersonRepository personRepository)
        {
            _personRepository = personRepository;
            _snapshotStore = snapshotStore;
            _bbqRepository = bbqRepository;
        }

        [Function(nameof(RunModerateBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "churras/{id}/moderar")] HttpRequestData req, string id)
        {
            try
            {
                var bbqWillHappen = await req.Body<ModerateBbqRequest>();
                var lookups = await _snapshotStore.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();
                var bbqEvent = await _bbqRepository.GetAsync(id);

                if (lookups != null)
                {
                    var personsIsModerator = await _personRepository.GetAsync(lookups.ModeratorIds[0]);

                    if (personsIsModerator == null)
                        if (!personsIsModerator.IsCoOwner)
                            return await req.CreateResponse(HttpStatusCode.Unauthorized, "You must be Moderator");

                    if (bbqEvent == null)
                        return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded to moderate");
                    else
                    {
                        var @event = new BbqStatusUpdated { GonnaHappen = bbqWillHappen.GonnaHappen, TrincaWillPay = bbqWillHappen.TrincaWillPay };
                        if (bbqWillHappen.GonnaHappen)
                            bbqEvent.BbqStatus = BbqStatus.Confirmed;
                        else
                            bbqEvent.BbqStatus = BbqStatus.ItsNotGonnaHappen;

                        bbqEvent.Apply(@event);
                        await _bbqRepository.SaveAsync(bbqEvent);
                    }
                }
                else
                    return await req.CreateResponse(HttpStatusCode.NotFound, "Moderator Not Found");

                return await req.CreateResponse(HttpStatusCode.OK, bbqEvent.TakeSnapshot());

                //------TODO-------LookupsHasBeenCreated




                //await _bbqRepository.SaveAsync(bbqEvent);

                //if (bbqWillHappen.GonnaHappen && bbqWillHappen.TrincaWillPay)
                //{
                //    foreach (var personId in lookups.PeopleIds.Count())
                //    {
                //        var personsToInvite = await _personRepository.GetAsync(personId);
                //        if (personsToInvite != null)
                //        {
                //            var @event = new PersonHasBeenInvitedToBbq(bbqEvent.Id, bbqEvent.BbqDate, bbqEvent.Reason);
                //            personsToInvite.Apply(@event);
                //            await _personRepository.SaveAsync(personsToInvite);
                //        }
                //        else
                //            return await req.CreateResponse(HttpStatusCode.NotFound, "Barbeque Event Not Founded");
                //    }
                //}
                //else
                //{
                //    foreach (var personId in lookups.ModeratorIds)
                //    {
                //        var personsToDecline = await _personRepository.GetAsync(personId);
                //        personsToDecline.Apply(new InviteWasDeclined { InviteId = id, PersonId = personsToDecline.Id });
                //        await _personRepository.SaveAsync(personsToDecline);
                //    }
                //}


            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
