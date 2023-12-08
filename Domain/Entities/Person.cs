using Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities
{
    public class Person : AggregateRoot
    {
        public string Name { get; set; }
        public bool IsCoOwner { get; set; }
        public IEnumerable<Invite> Invites { get; set; }
        public Person()
        {
            Invites = new List<Invite>();
        }
        public void When(PersonHasBeenCreated @event)
        {
            Id = @event.Id;
            Name = @event.Name;
            IsCoOwner = @event.IsCoOwner;
        }
        public void When(PersonHasBeenInvitedToBbq @event)
        {
            Invites = Invites.Append(new Invite
            {
                Id = @event.Id,
                InviteDate = @event.Date,
                BbqId = $"{@event.Date} - {@event.Reason}",
                InviteStatus = InviteStatus.Pending
            });
        }

        public void When(InviteWasAccepted @event)
        {
            var invite = Invites.FirstOrDefault(x => x.Id == @event.InviteId);
            invite.InviteStatus = InviteStatus.Accepted;
        }

        public void When(InviteWasDeclined @event)
        {
            var invite = Invites.FirstOrDefault(x => x.Id == @event.InviteId);
            
            if (invite == null) 
                return;
            
            invite.InviteStatus = InviteStatus.Declined;
        }

        public object? TakeSnapshot()
        {
            return new
            {
                Id,
                Name,
                IsCoOwner,
                Invites = Invites.Where(o => o.InviteStatus != InviteStatus.Declined)
                                .Where(o => o.InviteDate > DateTime.Now)
                                .Select(o => new { o.Id, o.BbqId, Status = o.InviteStatus.ToString() })
            };
        }
    }
}
