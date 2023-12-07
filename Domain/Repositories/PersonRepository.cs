using System;
using Domain.Entities;

namespace Domain.Repositories
{
    internal class PersonRepository : StreamRepository<Person>, IPersonRepository
    {
        public PersonRepository(IEventStore<Person> eventStore) : base(eventStore) { }
    }
}
