namespace Domain.Events
{
    public class LookupsHasBeenCreated : IEvent
    {
        public string Id { get; }

        public LookupsHasBeenCreated(string id)
        {
            Id = id;
        }
    }
}
