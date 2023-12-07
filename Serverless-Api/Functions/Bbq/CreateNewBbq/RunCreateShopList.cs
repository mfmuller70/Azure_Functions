using Domain.Entities;
using Domain.Repositories;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Serverless_Api
{
    public partial class RunCreateShopList
    {
        private readonly Person _user;
        private readonly IBbqRepository _bbqRepository;
        private readonly IPersonRepository _personsRepository;
        public RunCreateShopList(IBbqRepository bbqRepository, Person user, IPersonRepository personsRepository)
        {
            _user = user;
            _bbqRepository = bbqRepository;
            _personsRepository = personsRepository;
        }

        [Function(nameof(RunCreateShopList))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{personid}/shoplist")] HttpRequestData req, string inviteId)
        {
            try
            {
                var person = await _personsRepository.GetAsync(_user.Id);

                if (person == null)
                    return await req.CreateResponse(HttpStatusCode.NotFound, "Person not found.");

                if (!person.IsCoOwner)
                    return await req.CreateResponse(HttpStatusCode.Unauthorized, "You must be the owner.");

                var bbq = await _bbqRepository.GetAsync(inviteId);

                if (bbq == null)
                    return await req.CreateResponse(HttpStatusCode.NotFound, "You must create a barbecue event first.");

                return await req.CreateResponse(HttpStatusCode.Created, bbq.TakeSnapshot());
            }
            catch (Exception ex)
            {
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
            }
        }
    }
}
