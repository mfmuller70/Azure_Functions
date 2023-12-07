using System;

namespace Domain.Entities
{
    public class Invite
    {
        public string Id { get; set; }
        public string Bbq { get; set; }
        public InviteStatus Status { get; set; }
        public DateTime Date { get; set; }
    }

    public enum InviteStatus
    {
        Pending,
        Accepted,
        Declined
    }
}
