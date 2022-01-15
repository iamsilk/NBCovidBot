using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider : IDisposable
    {
        private readonly ActionScheduler _actionScheduler;
        private readonly ILogger<CovidDataProvider> _logger;
        private readonly CovidDataDbContext _dbContext;
        private readonly IConfiguration _configuration;

        private readonly List<HealthZone> _healthZones;

        private List<ZoneDailyInfo> _zonesDailyInfo;
        private ProvinceDailyInfo _provinceDailyInfo;
        private List<ProvincePastInfo> _provincePastInfo;
        private List<ZoneRecoveryPhaseInfo> _zonesRecoveryPhaseInfo;
        private ProvinceVaccineInfo _provinceVaccineInfo;

        private readonly List<Func<DataUpdateCallback>> _dataUpdateTasks;

        private const string DataQueryActionKey = nameof(CovidDataProvider) + "-Query";

        public CovidDataProvider(ActionScheduler actionScheduler,
            ILogger<CovidDataProvider> logger,
            CovidDataDbContext dbContext,
            IConfiguration configuration)
        {
            _actionScheduler = actionScheduler;
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;

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

            _dataUpdateTasks = new List<Func<DataUpdateCallback>>();

            _actionScheduler.ScheduleAction(DataQueryActionKey, _configuration["CovidQuerySchedule"], QueryUntilDataUpdates);

            RunOnDataUpdated(() => RecordUpdateToDatabase);

            // Get data now
            Task.Run(() => UpdateData(true));
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

        public ProvinceVaccineInfo GetProvinceVaccineInfo() => _provinceVaccineInfo;

        public delegate Task DataUpdateCallback(bool forced);

        public void RunOnDataUpdated(Func<DataUpdateCallback> task)
        {
            _dataUpdateTasks.Add(task);
        }

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

            if (root?.Features == null || root.Features.All(x => x?.Target == null))
            {
                _logger.LogWarning($"Server returned unexpected result. Could not parse {typeof(T).Name}");

                return null;
            }

            return root.Features.Select(x => x?.Target).Where(x => x != null).ToList();
        }

        private async Task<T> QuerySingle<T>(string service, string query, string orderBy = null) where T : class =>
            (await QueryMultiple<T>(service, query, orderBy))?.First();

        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private Task _queryLoop;

        private async Task QueryUntilDataUpdates()
        {
            lock (_cancellationToken)
            {
                _cancellationToken?.Cancel();
                _cancellationToken = new CancellationTokenSource();
            }

            _queryLoop?.Wait();

            async Task QueryLoopTask(CancellationToken cancellationToken)
            {
                while (!await UpdateData())
                {
                    var delayTask = Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                    await delayTask;

                    if (delayTask.IsCanceled) break;

                    _logger.LogInformation("Data was not updated. Waiting five minutes and checking again.");
                }
            };

            lock (_cancellationToken)
            {
                _queryLoop = QueryLoopTask(_cancellationToken.Token);
            }
            await _queryLoop;
        }

        private async Task<bool> UpdateData(bool forced = false)
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
                    zoneDailyInfo.ZoneNumber = zone.ZoneNumber;
                    zoneDailyInfo.ZoneTitle = zone.Title;

                    zones.Add(zoneDailyInfo);
                }
            }

            var province = await QuerySingle<ProvinceDailyInfo>("HealthZones", "HealthZone='Province'");

            if (province == null)
            {
                _logger.LogWarning("Unable to retrieve province data.");
                cancel = true;
            }

            var past = await QueryMultiple<ProvincePastInfo>("Covid19DailyCaseStats3", "Total>0", "DATE desc");

            if (past == null || past.Count == 0)
            {
                _logger.LogWarning("Unable to retrieve past provincial daily case stats.");
                cancel = true;
            }

            var zonesRecovery =
                await QueryMultiple<ZoneRecoveryPhaseInfo>("COVID19_AlertLevel", "'1'='1'", resultRecordCount: 2000);

            if (zonesRecovery == null || zonesRecovery.Count == 0)
            {
                _logger.LogWarning("Unable to retrieve zone recovery information.");
                cancel = true;
            }

            var provinceVaccineInfo = await QuerySingle<ProvinceVaccineInfo>("Covid19VaccineData2", "'1'='1'");

            if (provinceVaccineInfo == null)
            {
                _logger.LogWarning("Unable to retrieve province vaccine information.");
                cancel = true;
            }

            if (cancel) return false;

            if (_provinceDailyInfo != null && _provinceDailyInfo.LastUpdate == province.LastUpdate) return false;

            _zonesDailyInfo = zones;
            _provinceDailyInfo = province;
            _provincePastInfo = past;
            _zonesRecoveryPhaseInfo = zonesRecovery;
            _provinceVaccineInfo = provinceVaccineInfo;

            foreach (var task in _dataUpdateTasks)
            {
                try
                {
                    await task()(forced);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred when running data update task.");
                }
            }

            return true;
        }

        public Task<bool> ForceUpdateData() => UpdateData(true);

        private async Task RecordUpdateToDatabase(bool forced)
        {
            void CopyProperties<T>(T source, T target)
            {
                foreach (var property in typeof(T).GetProperties())
                {
                    property.SetValue(target, property.GetValue(source));
                }
            }

            // Province data

            var province = GetProvinceDailyInfo();

            await _dbContext.ProvinceData.LoadAsync();

            var existingProvince = await _dbContext.ProvinceData.FindAsync(province.ZoneNumber, province.LastUpdate);

            if (existingProvince != null)
            {
                CopyProperties(province, existingProvince);
            }
            else
            {
                await _dbContext.ProvinceData.AddAsync(province);
            }

            // Zone data

            var zones = GetZonesDailyInfo();

            await _dbContext.ZoneData.LoadAsync();

            foreach (var zone in zones)
            {
                var existingZone = await _dbContext.ZoneData.FindAsync(zone.ZoneNumber, zone.LastUpdate);

                if (existingZone != null)
                {
                    CopyProperties(zone, existingZone);
                }
                else
                {
                    await _dbContext.ZoneData.AddAsync(zone);
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
