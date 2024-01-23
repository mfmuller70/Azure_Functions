using System;

namespace Domain.Events
{
    public class ThereIsSomeoneElseInTheMood : IEvent
    {
        public ThereIsSomeoneElseInTheMood(Guid id, DateTime date, string reason, bool isValidPaying)
        {
            Id = id;
            Date = date;
            Reason = reason;
            IsValidPaying = isValidPaying;
        }

        public Guid Id { get; set; }
        public string Reason { get; set; }
        public bool IsValidPaying { get; set; }
        public DateTime Date { get; set; }
    }
}
