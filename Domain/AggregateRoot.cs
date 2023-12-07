using System;
using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using System.Runtime.ExceptionServices;
using Eveneum;

namespace Domain
{
    public abstract class AggregateRoot
    {
        public string Id { get; set; }
        public ulong Version { get; private set; }
        public List<IEvent> Changes { get; }

        public AggregateRoot()
        {
            Changes = new List<IEvent>();
        }

        public void Rehydrate(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                Mutate(@event);
                Version += 1;
            }
        }

        public void Apply(IEvent @event)
        {
            Changes.Add(@event);
            Mutate(@event);
        }

        private void Mutate(IEvent @event)
        {
            try
            {
                ((dynamic)this).When((dynamic)@event);
            }
            catch (RuntimeBinderException ex)
            {
                Console.WriteLine($"@@@@@@@ When({@event.GetType()} @event) method must be implemented for type {GetType()}. @@@@@@@");
                ExceptionDispatchInfo.Capture(new RuntimeBinderException(string.Format("Must implement event handler", @event.GetType(), GetType()), ex)).Throw();
            }
        }
    }
}
