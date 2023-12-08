using Eveneum;
using System.Net;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Azure.Messaging.ServiceBus;
using log4net;
using Domain.Repositories;
using System;

namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        private readonly Person _user;
        private readonly SnapshotStore _snapshots;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;


        public RunCreateNewBbq(IPersonRepository personRepository, IBbqRepository bbqRepository, SnapshotStore snapshots, Person user)
        {
            _user = user;
            _snapshots = snapshots;
            _bbqRepository= bbqRepository;
            _personRepository= personRepository;
        }

        [Function(nameof(RunCreateNewBbq))]
        //[StorageAccount("DeadLetterStorage")] 
        //podiamos ter um seviço de sercive bus para controlar as triggers das functions e deadeletter abaixo tambem
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "churras")] HttpRequestData req)
        {
            try
            {
                var input = await req.Body<NewBbqRequest>();

                if (input == null)
                {
                    //_logger.LogError("Error: RunCreateNewBbq", "input is required.");
                    return await req.CreateResponse(HttpStatusCode.BadRequest, "input is required.");
                }

                var churras = new Bbq();
                churras.Apply(new ThereIsSomeoneElseInTheMood(Guid.NewGuid(), input.Date, input.Reason, input.IsTrincasPaying));

                var churrasSnapshot = churras.TakeSnapshot();

                var Lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

                await _bbqRepository.SaveAsync(churras);

                //_logger.LogInformation("churras Apply OK");

                foreach (var personId in Lookups.ModeratorIds)
                {
                    var PersonHasBeenInvitedToBbq = await _personRepository.GetAsync(personId);

                    if (PersonHasBeenInvitedToBbq != null)
                    {
                        PersonHasBeenInvitedToBbq.Apply(new PersonHasBeenInvitedToBbq(churras.Id, churras.BbqDate, churras.Reason));
                        await _personRepository.SaveAsync(PersonHasBeenInvitedToBbq);
                    }
                    else
                    {
                       // _logger.LogInformation("No Persons to Apply BbqId");
                        return await req.CreateResponse(HttpStatusCode.PreconditionFailed, "No Persons to Apply BbqId.");
                    }
                }

                return await req.CreateResponse(HttpStatusCode.Created, churrasSnapshot);
            }
            catch (Exception ex)
            {
                //podiamos usar um REDIS para construir msgs customizadas (ja fiz isso em outro proejto)
               // _logger.LogError("RunCreateNewBbq",ex.Message.ToString(), ex.StackTrace);
                return await req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString()); //teriamos que tratar somente os tipos de erros
                //await deadLetterMessages.AddAsync(new DeadLetterMessage { Exception = ex, EventData = message });
                //aqui teriamos o controle de messages que não subiram por algum tipo de erro
            }
        }
    }
}
