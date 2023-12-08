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
        private readonly Person _user;
        private readonly IPersonRepository _repository;
        private readonly IBbqRepository _bbqRepository;

        public RunDeclineInvite(Person user, IPersonRepository repository, IBbqRepository bbqRepository)
        {
            _user = user;
            _repository = repository;
            _bbqRepository = bbqRepository;
        }

        [Function(nameof(RunDeclineInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/decline")] HttpRequestData req, string inviteId)
        {
            try
            {
                var inviteToVeg = await req.Body<InviteAnswer>();

                var person = await _repository.GetAsync(_user.Id);

                if (person == null)
                    return req.CreateResponse(System.Net.HttpStatusCode.NoContent);

                if (person.Invites.Where(o => o.Id == inviteId && o.InviteStatus == InviteStatus.Declined).Any())
                    return await req.CreateResponse(HttpStatusCode.Forbidden, "invitation has declined");

                var @event = new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id, IsVeg = inviteToVeg.IsVeg };

                person.Apply(@event);
                await _repository.SaveAsync(person);

                var bbq = await _bbqRepository.GetAsync(inviteId);
                bbq.Apply(@event);
                await _bbqRepository.SaveAsync(bbq);

                return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
