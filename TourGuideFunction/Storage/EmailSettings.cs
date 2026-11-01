using Azure;
using Azure.Data.Tables;

namespace TourGuideFunction.Storage
{
    public class EmailSettings : ITableEntity
    {
        public const string Partition = "settings";
        public const string Key = "email";

        public string PartitionKey { get; set; } = Partition;
        public string RowKey { get; set; } = Key;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Sender { get; set; } = "";
        /// <summary>Comma-separated addresses.</summary>
        public string Recipients { get; set; } = "";
    }
}
