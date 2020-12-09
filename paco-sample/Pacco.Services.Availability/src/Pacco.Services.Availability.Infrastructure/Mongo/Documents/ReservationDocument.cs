namespace Pacco.Services.Availability.Infrastructure.Mongo.Documents
{
    internal sealed class ReservationDocument
    {
        //https://stackoverflow.com/questions/6036433/datetime-issues-with-mongo-and-c-sharp
        //mongo keep data as UTC and we should care about it or use a timestamp
        
        public int TimeStamp { get; set; }
        public int Priority { get; set; }
    }
}