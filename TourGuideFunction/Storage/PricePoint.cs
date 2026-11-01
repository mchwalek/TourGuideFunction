using Azure;
using Azure.Data.Tables;
using TourGuideFunction.Excursions;

namespace TourGuideFunction.Storage
{
    public class PricePoint : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int Price { get; set; }
        public string DateFrom { get; set; } = "";
        public string DateTo { get; set; } = "";
        public string Origin { get; set; } = "";

        public static PricePoint Create(string excursionId, DateTimeOffset scrapedAt, Excursion excursion) => new()
        {
            PartitionKey = excursionId,
            RowKey = scrapedAt.UtcDateTime.ToString("yyyyMMddHHmmss"),
            Price = excursion.Price,
            DateFrom = excursion.DateFrom.ToString("yyyy-MM-dd"),
            DateTo = excursion.DateTo.ToString("yyyy-MM-dd"),
            Origin = excursion.OriginCity,
        };
    }
}
