using System;

namespace Domain.Events
{
    public class PersonHasBeenInvitedToBbq : IEvent
    {
        public string Id { get; }
        public DateTime Date { get; }
        public string Reason { get; }

        public PersonHasBeenInvitedToBbq(string id, DateTime date, string reason)
        {
            Id = id;
            Date = date;
            Reason = reason;
        }
    }
}
