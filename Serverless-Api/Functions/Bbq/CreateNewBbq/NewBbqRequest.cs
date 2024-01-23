namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        public class NewBbqRequest
        {
            public NewBbqRequest(DateTime date, string reason, bool isValidPaying)
            {
                Date = date;
                Reason = reason;
                IsValidPaying = isValidPaying;
            }

            public DateTime Date { get; set; }
            public string Reason { get; set; }
            public bool IsValidPaying { get; set; }
        }
    }
}
