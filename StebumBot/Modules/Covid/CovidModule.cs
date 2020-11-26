using Discord.Commands;
using JetBrains.Annotations;
using StebumBot.Modules.Covid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace StebumBot.Modules.Covid
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CovidModule : ModuleBase<SocketCommandContext>
    {
        private async Task<List<T>> QueryData<T>(string service, string query, string orderBy = null)
        {
            var fields = new List<string>();

            foreach (var property in typeof(T).GetProperties())
            {
                var propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>();

                if (propertyName == null) continue;

                fields.Add(propertyName.Name);
            }

            var requestParams = new Dictionary<string, object>()
            {
                {"f", "json"},
                {"where", query},
                {"returnGeometry", false},
                {"outFields", string.Join(',', fields)},
                {"resultOffset", 0},
                {"resultRecordCount", 50},
                {"resultType", "standard"},
                {"cacheHint", true}
            };

            if (orderBy != null)
                requestParams.Add("orderByFields", orderBy);

            string requestUrl =
                $"https://services5.arcgis.com/WO0dQcVbxj7TZHkH/arcgis/rest/services/{service}/FeatureServer/0/query?";


            using var client = new HttpClient();

            var response = await client.GetAsync(requestUrl + string.Join('&',
                requestParams.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value.ToString()))));

            if (!response.IsSuccessStatusCode)
            {
                await ReplyAsync(
                    $"Server returned unsuccessful response code {typeof(T).Name}: {response.StatusCode} ({(int)response.StatusCode})");

                return null;
            }

            Root<T> root;

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync();

                root = await JsonSerializer.DeserializeAsync<Root<T>>(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            if (root?.Features == null || root.Features.Count(x => x != null) == 0)
            {
                await ReplyAsync($"Server returned unexpected result. Could not parse {typeof(T).Name}");

                return null;
            }

            return root.Features.Select(x => x.Target).Where(x => x != null).ToList();
        }

        private DateTimeOffset GetDate(long timestamp)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time");
            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return TimeZoneInfo.ConvertTime(date, timeZone);
        }

        [Command("covid")]
        [Summary("View COVID stats")]
        public async Task CovidAsync()
        {
            var province = (await QueryData<DailyProvinceCovidInfo>("HealthZones", "HealthZone='Province'"))?.First();
            if (province == null) return;

            var city = (await QueryData<DailyCityCovidInfo>("HealthZones", "HealthZone='2'"))?.First();
            if (city == null) return;

            var pastWeek = await QueryData<PastCovidInfo>("Covid19DailyCaseStats", "Total>0", "DATE desc");
            if (pastWeek == null) return;

            if (pastWeek.Count < 8)
            {
                await ReplyAsync("Could not find past weeks data");
                return;
            }

            var changes = new int[7];
            var history = new string[7];

            for (int i = 0; i < 7; i++)
            {
                changes[i] = pastWeek[i].Active - pastWeek[i + 1].Active;

                var date = GetDate(pastWeek[i].UnixTimestamp);

                history[i] = $"{date:MMM dd} - *{pastWeek[i].Active} ({(changes[0] >= 0 ? "+" : "")}{changes[i]})*";
            }

            var latestDate = GetDate(province.LastUpdate);

            var nl = Environment.NewLine;

            var response = $"__**{latestDate:MMMM dd, yyyy} (Provincial / Saint John):**__" + nl +
                           $"Active Cases: *{province.ActiveCases} ({(changes[0] >= 0 ? "+" : "")}{changes[0]})  /  {city.ActiveCases}*" + nl +
                           $"New: *{province.NewToday}  /  {city.NewToday}*   Recovered: *{pastWeek[0].NewRecoveredToday}*" + nl +
                           $"Recovery Phase: *{province.RecoveryPhase}  /  {city.RecoveryPhase}*" + nl +
                           nl +
                           $"Total Cases: *{province.TotalCases}*" + nl +
                           $"Travel Related: *{province.TravelRel}*" + nl +
                           $"Close Contact: *{province.CloseContact}*" + nl +
                           $"Community Transmission: *{province.CommTransmission}*" + nl +
                           $"Under Investigation: *{province.UnderInvestigation}*" + nl +
                           nl +
                           "__**Past Week:**__" + nl +
                           string.Join(nl, history);

            await ReplyAsync(response);
        }
    }
}
