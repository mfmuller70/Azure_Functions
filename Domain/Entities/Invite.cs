using System;

namespace Domain.Entities
{
    public class Invite
    {
        public string Id { get; set; }
        public string BbqId { get; set; }
        public InviteStatus InviteStatus { get; set; }
        public DateTime InviteDate { get; set; }
    }

    public enum InviteStatus
    {
        Pending,
        Accepted,
        Declined
    }
}
