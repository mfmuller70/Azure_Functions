namespace Domain.Events
{
    public class PersonHasBeenCreated : IEvent
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsCoOwner { get; }

        public PersonHasBeenCreated(string id, string name, bool isCoOwner)
        {
            Id = id;
            Name = name;
            IsCoOwner = isCoOwner;
        }
    }
}
