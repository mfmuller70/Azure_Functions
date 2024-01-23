using Domain;
using Eveneum;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using static Domain.ServiceCollectionExtensions;
using static Serverless_Api.RunAcceptInvite;
using System.Net;

namespace Serverless_Api
{
    public partial class RunDeclineInvite
    {
        private readonly Person _person;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;

        public RunDeclineInvite(Person person, 
                                IPersonRepository personRepository, 
                                IBbqRepository bbqRepository)
        {
            _person = person;
            _personRepository = personRepository;
            _bbqRepository = bbqRepository;
        }

        [Function(nameof(RunDeclineInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/decline")] HttpRequestData req, string inviteId)
        {
            try
            {
                var inviteToVeg = await req.Body<InviteAnswer>();
                var person = await _personRepository.GetAsync(_person.Id);

                if (person == null)
                    return await req.CreateResponse(HttpStatusCode.NotFound, "Person not founded");

                if (person.Invites.Where(o => o.Id == inviteId && o.InviteStatus == InviteStatus.Declined).Any())
                    return await req.CreateResponse(HttpStatusCode.Forbidden, "invitation has declined");

                var @event = new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id, IsVeg = inviteToVeg.IsVeg };

                person.Apply(@event);
                await _personRepository.SaveAsync(person);

                var bbq = await _bbqRepository.GetAsync(inviteId);

                if (bbq.NumberPersonsConfirmation < 7)
                    bbq.BbqStatus = BbqStatus.PendingConfirmations;

                bbq.Apply(@event);
                await _bbqRepository.SaveAsync(bbq);

                return await req.CreateResponse(HttpStatusCode.OK, person.TakeSnapshot());
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
