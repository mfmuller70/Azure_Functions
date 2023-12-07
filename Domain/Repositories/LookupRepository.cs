using System;
using Domain.Entities;

namespace Domain.Repositories
{
    internal class LookupRepository : StreamRepository<Lookups>, ILookupsRepository
    {
        public LookupRepository(IEventStore<Lookups> eventStore) : base(eventStore) { }
    }
}
