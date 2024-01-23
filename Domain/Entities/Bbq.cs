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
        public BbqStatus BbqStatus { get; set; }
        public DateTime BbqDate { get; set; }
        public bool IsValidPaying { get; set; }
        public int NumberPersonsConfirmation { get; set; }
        public List<BbqBasketList> BbqBasketList { get; set; } = new ();

        public void When(ThereIsSomeoneElseInTheMood @event)
        {
            Id = @event.Id.ToString();
            BbqDate = @event.Date;
            Reason = @event.Reason;
            BbqStatus = BbqStatus.New;
        }

        public void When(BbqStatusUpdated @event)
        {
            if (@event.GonnaHappen)
                BbqStatus = BbqStatus.PendingConfirmations;
            else 
                BbqStatus = BbqStatus.ItsNotGonnaHappen;

            if (@event.ValidWillPay)
                IsValidPaying = true;
        }

        public void When(InviteWasDeclined @event)
        {
            //TODO:Deve ser possível rejeitar um convite já aceito antes.
            //Se este for o caso, a quantidade de comida calculada pelo aceite anterior do convite
            //deve ser retirado da lista de compras do churrasco.

            NumberPersonsConfirmation -= 1;
            BbqBasketList bbqBasketList = new();
            bbqBasketList.Salad = @event.IsVeg ? bbqBasketList.Salad = - 500 : bbqBasketList.Salad = - 250; //gramas por pessoa
            bbqBasketList.Steak = @event.IsVeg ? 0 : bbqBasketList.Salad = -350; //gramas por pessoa

            BbqBasketList.Add(bbqBasketList);

            if (NumberPersonsConfirmation < 7 && BbqStatus != BbqStatus.ItsNotGonnaHappen)
                BbqStatus = BbqStatus.PendingConfirmations;  

        }

        public void When(InviteWasAccepted @event)
        {
            //Se ao rejeitar, o número de pessoas confirmadas no churrasco for menor que sete,
            if (NumberPersonsConfirmation == 7 && BbqStatus == BbqStatus.Confirmed)
                BbqStatus = BbqStatus.Confirmed;  //o churrasco deverá ter seu status atualizado para “Pendente de confirmações”.
            else
            {
                NumberPersonsConfirmation += 1;
                BbqBasketList bbqBasketList = new();
                bbqBasketList.Salad = @event.IsVeg ? bbqBasketList.Salad = +500 : bbqBasketList.Salad = +250; //gramas por pessoa
                bbqBasketList.Steak = @event.IsVeg ? 0 : bbqBasketList.Salad = +350; //gramas por pessoa

                BbqBasketList.Add(bbqBasketList);
            }
        }

        public object TakeSnapshot()
        {
            return new
            {
                Id,
                BbqDate,
                IsValidPaying,
                Status = BbqStatus.ToString(),
                NumberPersonsConfirmation,
                BbqBasketList = BbqBasketList.GroupBy(e => e.BbqId).Select(e => new
                {
                    Salad = e.Sum(e => e.Salad),
                    Steak = e.Sum(e => e.Steak)
                })
            };
        }
    }
}
