namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        public class NewBbqRequest
        {
            public NewBbqRequest(DateTime date, string reason, bool isTrincaPaying)
            {
                Date = date;
                Reason = reason;
                IsTrincasPaying = isTrincaPaying;
            }

            public DateTime Date { get; set; }
            public string Reason { get; set; }
            public bool IsTrincasPaying { get; set; }
        }
    }
}
