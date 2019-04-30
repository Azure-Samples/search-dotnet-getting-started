namespace AzureSearch.SDKHowTo
{
    using System;
    using System.Text;

    public partial class Room
    {
        // This implementation of ToString() is only for the purposes of the sample console application.
        // You can override ToString() in your own model class if you want, but you don't need to in order
        // to use the Azure Search .NET SDK.
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!String.IsNullOrEmpty(Description))
            {
                builder.AppendFormat("Description: {0}\n", Description);
            }

            if (!String.IsNullOrEmpty(DescriptionFr))
            {
                builder.AppendFormat("DescriptionFr: {0}\n", DescriptionFr);
            }

            if (!String.IsNullOrEmpty(Type))
            {
                builder.AppendFormat("Type: {0}\n", Type);
            }

            if (BaseRate.HasValue)
            {
                builder.AppendFormat("BaseRate: {0}\n", BaseRate);
            }

            if (!String.IsNullOrEmpty(BedOptions))
            {
                builder.AppendFormat("BedOptions: {0}\n", BedOptions);
            }

            builder.AppendFormat("SleepsCount: {0}\n", BedOptions);

            if (SmokingAllowed.HasValue)
            {
                builder.AppendFormat("Smoking allowed: {0}\n", SmokingAllowed.Value ? "yes" : "no");
            }

            if (Tags != null && Tags.Length > 0)
            {
                builder.AppendFormat("Tags: [ {0} ]\n", String.Join(", ", Tags));
            }

            return builder.ToString();
        }
    }
}