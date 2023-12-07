using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Domain.Events;

namespace Domain.Entities
{
    public class Bbq : AggregateRoot
    {
        public string Reason { get; set; }
        public BbqStatus Status { get; set; }
        public DateTime Date { get; set; }
        public bool IsTrincasPaying { get; set; }
        public int NumberPersonsConfirmation { get; set; }
        public BbqShoppingList BbqShoppingList { get; set; } = new BbqShoppingList();

        public void When(ThereIsSomeoneElseInTheMood @event)
        {
            Id = @event.Id.ToString();
            Date = @event.Date;
            Reason = @event.Reason;
            Status = BbqStatus.New;
        }

        public void When(BbqStatusUpdated @event)
        {
            if (@event.GonnaHappen)
                Status = BbqStatus.PendingConfirmations;
            else 
                Status = BbqStatus.ItsNotGonnaHappen;

            if (@event.TrincaWillPay)
                IsTrincasPaying = true;
        }

        public void When(InviteWasDeclined @event)
        {
            //TODO:Deve ser possível rejeitar um convite já aceito antes.
            //Se este for o caso, a quantidade de comida calculada pelo aceite anterior do convite
            //deve ser retirado da lista de compras do churrasco.

            NumberPersonsConfirmation -= 1;
            BbqShoppingList.Salad = @event.IsVeg ? -500 : -250; //gramas por pessoa
            BbqShoppingList.Steak = @event.IsVeg ? 0 : -350; //gramas por pessoa

            //Se ao rejeitar, o número de pessoas confirmadas no churrasco for menor que sete,
            if (NumberPersonsConfirmation < 7 && Status != BbqStatus.PendingConfirmations)
                Status = BbqStatus.PendingConfirmations;  //o churrasco deverá ter seu status atualizado para “Pendente de confirmações”.

        }

        public void When(InviteWasAccepted @event)
        {
            //Se ao rejeitar, o número de pessoas confirmadas no churrasco for menor que sete,
            if (NumberPersonsConfirmation == 7 && Status == BbqStatus.Confirmed)
                Status = BbqStatus.Confirmed;  //o churrasco deverá ter seu status atualizado para “Pendente de confirmações”.
            else
            {
                NumberPersonsConfirmation += 1;
                BbqShoppingList.Salad = @event.IsVeg ? +500 : +250; //gramas por pessoa
                BbqShoppingList.Steak = @event.IsVeg ? 0 : +350; //gramas por pessoa
            }
        }

        public object TakeSnapshot()
        {
            return new
            {
                Id,
                Date,
                IsTrincasPaying,
                Status = Status.ToString(),
                NumberPersonsConfirmation,
                BbqShoppingList
            };
        }
    }
}
