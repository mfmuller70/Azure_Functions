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
        private readonly Person _person;
        private readonly SnapshotStore _snapshotStore;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;
       // private readonly ILogger _logger;   


        public RunCreateNewBbq(IPersonRepository personRepository, 
                    IBbqRepository bbqRepository, 
                    SnapshotStore snapshotStore, 
                    Person person)
        {
            _person = person;
            _snapshotStore = snapshotStore;
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
                var BbqEventInformation = await req.Body<NewBbqRequest>();

                if (BbqEventInformation == null)
                    return await req.CreateResponse(HttpStatusCode.BadRequest, "Details about Barbeque is mandatory.");

                var churras = new Bbq();
                churras.Apply(new ThereIsSomeoneElseInTheMood(Guid.NewGuid(), BbqEventInformation.Date, BbqEventInformation.Reason, BbqEventInformation.IsValidPaying));

                var churrasSnapshot = churras.TakeSnapshot();

                var Lookups = await _snapshotStore.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

                await _bbqRepository.SaveAsync(churras);

                //LookupsHasBeenCreated

                foreach (var personId in Lookups.ModeratorIds)
                {
                    var PersonHasBeenInvitedToBbq = await _personRepository.GetAsync(personId);

                    if (PersonHasBeenInvitedToBbq != null)
                    {
                        PersonHasBeenInvitedToBbq.Apply(new PersonHasBeenInvitedToBbq(churras.Id, churras.BbqDate, churras.Reason));
                        await _personRepository.SaveAsync(PersonHasBeenInvitedToBbq);
                    }
                    else
                        return await req.CreateResponse(HttpStatusCode.PreconditionFailed, "No Persons to Apply BbqId.");
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
