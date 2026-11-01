using Azure;
using Azure.Data.Tables;

namespace TourGuideFunction.Storage
{
    public static class ExcursionStatus
    {
        public const string Available = "Available";
        public const string Unavailable = "Unavailable";
    }

    // One row per watched offer, edited by hand (e.g. in Azure Storage Explorer).
    public class TrackedExcursion : ITableEntity
    {
        public const string Partition = "excursion";

        public string PartitionKey { get; set; } = Partition;
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Url { get; set; } = "";
        /// <summary>yyyy-MM-dd</summary>
        public string ExpectedDateFrom { get; set; } = "";
        public string ExpectedOrigin { get; set; } = "";
        public int Threshold { get; set; }
        public bool Enabled { get; set; }
        /// <summary>Written by the app; see <see cref="ExcursionStatus"/>.</summary>
        public string? LastStatus { get; set; }
    }
}
