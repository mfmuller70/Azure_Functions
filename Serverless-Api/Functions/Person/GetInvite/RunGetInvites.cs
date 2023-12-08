using Domain;
using Domain.Entities;
using Domain.Repositories;
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

        public RunGetInvites(Person user, IPersonRepository repository, IBbqRepository bbqs)
        {
            _user = user;
            _repository = repository;
            _bbqs = bbqs;
        }

        [Function(nameof(RunGetInvites))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/invites")] HttpRequestData req)
        {
            try
            {
                var snapshots = new List<object>();
                var moderator = await _repository.GetAsync(_user.Id);
                foreach (var bbqId in moderator.Invites.Where(i => i.InviteDate > DateTime.Now).Select(o => o.Id).ToList())
                {
                    var bbq = await _bbqs.GetAsync(bbqId);
                    snapshots.Add(bbq.TakeSnapshot());
                }

                return await req.CreateResponse(HttpStatusCode.OK, snapshots);
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
