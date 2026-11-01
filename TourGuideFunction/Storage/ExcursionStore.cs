using Azure;
using Azure.Data.Tables;

namespace TourGuideFunction.Storage
{
    public class ExcursionStore
    {
        public const string ExcursionsTable = "Excursions";
        public const string SettingsTable = "Settings";
        public const string PricePointsTable = "PricePoints";

        private readonly TableClient _excursions;
        private readonly TableClient _settings;
        private readonly TableClient _pricePoints;

        public ExcursionStore(TableServiceClient service)
        {
            _excursions = service.GetTableClient(ExcursionsTable);
            _settings = service.GetTableClient(SettingsTable);
            _pricePoints = service.GetTableClient(PricePointsTable);
        }

        public Task InitializeAsync() => Task.WhenAll(
            _excursions.CreateIfNotExistsAsync(),
            _settings.CreateIfNotExistsAsync(),
            _pricePoints.CreateIfNotExistsAsync());

        public async Task<IReadOnlyList<TrackedExcursion>> GetEnabledExcursionsAsync()
        {
            var result = new List<TrackedExcursion>();
            await foreach (var e in _excursions.QueryAsync<TrackedExcursion>(e => e.PartitionKey == TrackedExcursion.Partition && e.Enabled))
                result.Add(e);
            return result;
        }

        public async Task<EmailSettings?> GetEmailSettingsAsync()
        {
            var response = await _settings.GetEntityIfExistsAsync<EmailSettings>(EmailSettings.Partition, EmailSettings.Key);
            return response.HasValue ? response.Value : null;
        }

        // shortcut: scans the excursion's whole partition (one point per day), store a running minimum if it grows to many thousands.
        public async Task<int?> GetLowestPriceAsync(string excursionId)
        {
            int? lowest = null;
            await foreach (var p in _pricePoints.QueryAsync<PricePoint>(p => p.PartitionKey == excursionId, select: new[] { nameof(PricePoint.Price) }))
                lowest = lowest is null ? p.Price : Math.Min(lowest.Value, p.Price);
            return lowest;
        }

        public Task AddPricePointAsync(PricePoint point) => _pricePoints.AddEntityAsync(point);

        public Task SetLastStatusAsync(string excursionId, string status) =>
            _excursions.UpdateEntityAsync(
                new TableEntity(TrackedExcursion.Partition, excursionId) { [nameof(TrackedExcursion.LastStatus)] = status },
                ETag.All,
                TableUpdateMode.Merge);
    }
}
