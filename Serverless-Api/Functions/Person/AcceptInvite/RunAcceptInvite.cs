using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Serverless_Api
{
    public partial class RunAcceptInvite
    {
        private readonly Person _user;
        private readonly IPersonRepository _repository;
        private readonly IBbqRepository _bbqRepository;
        public RunAcceptInvite(IPersonRepository repository, Person user,
                               IBbqRepository bbqRepository)
        {
            _user = user;
            _repository = repository;
            _bbqRepository = bbqRepository;
        }

        [Function(nameof(RunAcceptInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/accept")] HttpRequestData req, string inviteId)
        {
            try
            {
                var inviteToVeg = await req.Body<InviteAnswer>();
                var person = await _repository.GetAsync(_user.Id);

                var @event = new InviteWasAccepted { InviteId = inviteId, IsVeg = inviteToVeg.IsVeg, PersonId = person.Id };

                if (person.Invites.Where(o => o.Id == inviteId && o.InviteStatus == InviteStatus.Accepted).Any())
                    return await req.CreateResponse(HttpStatusCode.Forbidden, "invitation has already been accepted");

                person.Apply(new InviteWasAccepted { InviteId = inviteId, IsVeg = inviteToVeg.IsVeg, PersonId = person.Id });

                await _repository.SaveAsync(person);

                var bbq = await _bbqRepository.GetAsync(inviteId);
                bbq.Apply(@event);
                await _bbqRepository.SaveAsync(bbq);

                if (bbq.NumberPersonsConfirmation == 7) //maximo de invites possiveis
                {
                    bbq = await _bbqRepository.GetAsync(inviteId);
                    bbq.BbqStatus = BbqStatus.Confirmed;

                    await _bbqRepository.SaveAsync(bbq);
                }

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
