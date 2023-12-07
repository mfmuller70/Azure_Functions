using System;
using Domain.Entities;

namespace Domain.Repositories
{
    internal class BbqRepository : StreamRepository<Bbq>, IBbqRepository
    {
        public BbqRepository(IEventStore<Bbq> eventStore) : base(eventStore) { }
    }
}
