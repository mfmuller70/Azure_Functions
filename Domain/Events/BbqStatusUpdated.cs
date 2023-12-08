namespace Domain.Events
{
    public class BbqStatusUpdated : IEvent
    {
        public bool GonnaHappen { get; set; }
        public bool TrincaWillPay { get; set; }
    }
}
