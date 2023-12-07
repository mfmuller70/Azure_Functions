namespace Domain.Events
{
    public class InviteWasDeclined : IEvent
    {
        public string InviteId { get; set; }
        public string PersonId { get; set; }
        public bool IsVeg { get; set; }
    }
}
