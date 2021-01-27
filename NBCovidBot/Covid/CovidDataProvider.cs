using Microsoft.Extensions.Logging;
using NBCovidBot.Covid.Models;
using NBCovidBot.Scheduling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider : IDisposable
    {
        private readonly ActionScheduler _actionScheduler;
        private readonly ILogger<CovidDataProvider> _logger;
        private readonly List<HealthZone> _healthZones;

        private List<ZoneDailyInfo> _zonesDailyInfo;
        private ProvinceDailyInfo _provinceDailyInfo;
        private List<ProvincePastInfo> _provincePastInfo;
        private List<ZoneRecoveryPhaseInfo> _zonesRecoveryPhaseInfo;

        private const string DataQueryActionKey = nameof(CovidDataProvider) + "-Query";

        public CovidDataProvider(ActionScheduler actionScheduler, ILogger<CovidDataProvider> logger)
        {
            _actionScheduler = actionScheduler;
            _logger = logger;

            // ReSharper disable StringLiteralTypo
            _healthZones = new List<HealthZone>
            {
                new HealthZone("Moncton", 1),
                new HealthZone("Saint John", 2),
                new HealthZone("Fredericton", 3),
                new HealthZone("Edmundston", 4),
                new HealthZone("Campbellton", 5),
                new HealthZone("Bathurst", 6),
                new HealthZone("Miramichi", 7)
            };
            // ReSharper restore StringLiteralTypo

            _zonesDailyInfo = new List<ZoneDailyInfo>();
            _provincePastInfo = new List<ProvincePastInfo>();
            _zonesRecoveryPhaseInfo = new List<ZoneRecoveryPhaseInfo>();

            // Run data query every day at 3:10pm current timezone
            _actionScheduler.ScheduleAction(DataQueryActionKey, "10 15 * * *", ForceUpdateData);

            // Get data now
            Task.Run(ForceUpdateData);
        }

        public void Dispose()
        {
            _actionScheduler.UnscheduleAction(DataQueryActionKey);
        }

        public IReadOnlyCollection<HealthZone> GetHealthZones() => _healthZones.AsReadOnly();

        public int GetHealthZoneNumber(string title) =>
            _healthZones.First(x => x.Title.Equals(title, StringComparison.OrdinalIgnoreCase)).ZoneNumber;

        public string GetHealthZoneTitle(int zoneNumber) =>
            _healthZones.First(x => x.ZoneNumber == zoneNumber).Title;

        public IReadOnlyCollection<ZoneDailyInfo> GetZonesDailyInfo() => _zonesDailyInfo.AsReadOnly();

        public ProvinceDailyInfo GetProvinceDailyInfo() => _provinceDailyInfo;

        public IReadOnlyCollection<ProvincePastInfo> GetProvincePastInfo() => _provincePastInfo.AsReadOnly();

        public IReadOnlyCollection<ZoneRecoveryPhaseInfo> GetZonesRecoveryPhaseInfo() =>
            _zonesRecoveryPhaseInfo.AsReadOnly();
        
        private async Task<List<T>> QueryMultiple<T>(string service, string query, string orderBy = null, int resultRecordCount = 50) where T : class
        {
            var fields = new List<string>();
            
            foreach (var property in typeof(T).GetProperties())
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                var name = property.GetCustomAttribute<JsonPropertyNameAttribute>();

                if (name == null) continue;

                fields.Add(name.Name);
            }

            var requestParams = new Dictionary<string, object>()
            {
                {"f", "json"},
                {"where", query},
                {"returnGeometry", false},
                {"outFields", string.Join(',', fields)},
                {"resultOffset", 0},
                {"resultRecordCount", resultRecordCount},
                {"resultType", "standard"},
                {"cacheHint", true}
            };

            if (orderBy != null)
                requestParams.Add("orderByFields", orderBy);

            var requestUrl =
                $"https://services5.arcgis.com/WO0dQcVbxj7TZHkH/arcgis/rest/services/{service}/FeatureServer/0/query?";


            using var client = new HttpClient();

            requestUrl += string.Join('&',
                requestParams.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value.ToString())));

            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Server returned unsuccessful response code {typeof(T).Name}: {response.StatusCode} ({(int)response.StatusCode})");

                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();

            using var reader = new StreamReader(stream);

            var content = await reader.ReadToEndAsync();

            var root = JsonSerializer.Deserialize<Root<T>>(content);

            if (root?.Features == null || root.Features.Count(x => x?.Target != null) == 0)
            {
                _logger.LogWarning($"Server returned unexpected result. Could not parse {typeof(T).Name}");

                return null;
            }

            return root.Features.Select(x => x?.Target).Where(x => x != null).ToList();
        }

        private async Task<T> QuerySingle<T>(string service, string query, string orderBy = null) where T : class =>
            (await QueryMultiple<T>(service, query, orderBy))?.First();

        public async Task<bool> ForceUpdateData()
        {
            var cancel = false;

            var zones = new List<ZoneDailyInfo>();

            foreach (var zone in _healthZones)
            {
                var zoneDailyInfo = await QuerySingle<ZoneDailyInfo>("HealthZones", $"HealthZone='{zone.ZoneNumber}'");

                if (zoneDailyInfo == null)
                {
                    _logger.LogWarning($"Unable to retrieve daily zone data for zone {zone.Title} ({zone.ZoneNumber}).");
                    cancel = true;
                }
                else
                {
                    zoneDailyInfo.HealthZone = zone;

                    zones.Add(zoneDailyInfo);
                }
            }

            var province = await QuerySingle<ProvinceDailyInfo>("HealthZones", "HealthZone='Province'");

            if (province == null)
            {
                _logger.LogWarning("Unable to retrieve province data.");
                cancel = true;
            }

            var past = await QueryMultiple<ProvincePastInfo>("Covid19DailyCaseStats", "Total>0", "DATE desc");

            if (past == null || past.Count == 0)
            {
                _logger.LogWarning("Unable to retrieve past provincial daily case stats.");
                cancel = true;
            }

            var zonesRecovery =
                await QueryMultiple<ZoneRecoveryPhaseInfo>("RecoveryPhases", "'1'='1'", resultRecordCount: 4000);

            if (zonesRecovery == null || zonesRecovery.Count == 0)
            {
                _logger.LogWarning("Unable to retrieve zone recovery information.");
                cancel = true;
            }

            if (cancel) return false;

            _zonesDailyInfo = zones;
            _provinceDailyInfo = province;
            _provincePastInfo = past;
            _zonesRecoveryPhaseInfo = zonesRecovery;

            return true;
        }
    }
}
