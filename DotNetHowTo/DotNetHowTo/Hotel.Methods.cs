namespace AzureSearch.SDKHowTo
{
    using System;
    using System.Text;

    public partial class Hotel
    {
        // This implementation of ToString() is only for the purposes of the sample console application.
        // You can override ToString() in your own model class if you want, but you don't need to in order
        // to use the Azure Search .NET SDK.
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!String.IsNullOrEmpty(HotelId))
            {
                builder.AppendFormat("ID: {0}\t", HotelId);
            }

            if (!String.IsNullOrEmpty(Description))
            {
                builder.AppendFormat("Description: {0}\t", Description);
            }

            if (!String.IsNullOrEmpty(DescriptionFr))
            {
                builder.AppendFormat("Description (French): {0}\t", DescriptionFr);
            }

            if (!String.IsNullOrEmpty(HotelName))
            {
                builder.AppendFormat("Name: {0}\t", HotelName);
            }

            if (!String.IsNullOrEmpty(Category))
            {
                builder.AppendFormat("Category: {0}\t", Category);
            }

            if (Tags != null && Tags.Length > 0)
            {
                builder.AppendFormat("Tags: [{0}]\t", String.Join(", ", Tags));
            }

            if (ParkingIncluded.HasValue)
            {
                builder.AppendFormat("Parking included: {0}\t", ParkingIncluded.Value ? "yes" : "no");
            }

            if (LastRenovationDate.HasValue)
            {
                builder.AppendFormat("Last renovated on: {0}\t", LastRenovationDate);
            }

            if (Rating.HasValue)
            {
                builder.AppendFormat("Rating: {0}/5\t", Rating);
            }

            if (Location != null)
            {
                builder.AppendFormat("Location: Latitude {0}, longitude {1}\t", Location.Latitude, Location.Longitude);
            }

            //List the rooms.
            foreach (var room in Rooms)
            {
                if (!String.IsNullOrEmpty(room.Description))
                {
                    builder.AppendFormat("Description: {0}\t", room.Description);
                }

                if (!String.IsNullOrEmpty(room.DescriptionFr))
                {
                    builder.AppendFormat("Description (French): {0}\t", room.DescriptionFr);
                }

                if (!String.IsNullOrEmpty(room.Type))
                {
                    builder.AppendFormat("Room type: {0}\t", room.Type);
                }

                if (room.BaseRate.HasValue)
                {
                    builder.AppendFormat("Base rate: {0}\t", room.BaseRate);
                }

                if (!String.IsNullOrEmpty(room.BedOptions))
                {
                    builder.AppendFormat("Bed options: {0}\t", room.BedOptions);
                }

                if (room.SleepsCount > 0)
                {
                    builder.AppendFormat((room.SleepsCount > 1) ? "Sleeps {0} people\t" : "Sleeps {0} person\t", room.SleepsCount);
                }

                if (room.SmokingAllowed.HasValue)
                {
                    builder.AppendFormat((room.SmokingAllowed.Value) ? "Smoking room\t" : "Non-smoking room\ty");
                }
            }

            return builder.ToString();
        }
    }
}
