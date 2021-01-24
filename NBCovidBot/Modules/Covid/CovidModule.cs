using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NBCovidBot.Modules.Covid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Discord;
using TimeZoneConverter;

namespace NBCovidBot.Modules.Covid
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CovidModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;

        public CovidModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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

            await using var stream = await response.Content.ReadAsStreamAsync();

            var root = await JsonSerializer.DeserializeAsync<Root<T>>(stream);

            if (root?.Features == null || root.Features.Count(x => x != null) == 0)
            {
                await ReplyAsync($"Server returned unexpected result. Could not parse {typeof(T).Name}");

                return null;
            }

            return root.Features.Select(x => x.Target).Where(x => x != null).ToList();
        }

        private DateTimeOffset GetDate(long timestamp)
        {
            var timeZone = TZConvert.GetTimeZoneInfo(_configuration["timezone"]);
            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return TimeZoneInfo.ConvertTime(date, timeZone);
        }
        private string JoinColumns(int spacing, params string[][] columns)
        {
            var numCols = columns.Length;

            var maxRowLen = columns.Select(
                column => column.Select(row => row.Length).Prepend(0).Max()).ToArray();

            var numRows = columns.Max(x => x.Length);

            var builder = new StringBuilder();

            for (var i = 0; i < numRows; i++)
            {
                for (var j = 0; j < columns.Length; j++)
                {
                    var column = columns[j];

                    var spaces = maxRowLen[j] + spacing;

                    if (i < column.Length)
                    {
                        var content = column.ElementAt(i);
                        spaces -= content.Length;

                        builder.Append(content);
                    }

                    builder.Append(new string(' ', spaces));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        [Command("covid")]
        [Summary("View COVID stats")]
        public async Task CovidAsync()
        {
            var province = (await QueryData<DailyProvinceCovidInfo>("HealthZones", "HealthZone='Province'"))?.First();
            if (province == null) return;

            var pastWeek = await QueryData<PastCovidInfo>("Covid19DailyCaseStats", "Total>0", "DATE desc");
            if (pastWeek == null) return;

            if (pastWeek.Count < 8)
            {
                await ReplyAsync("Could not find past weeks data");
                return;
            }

            var changes = new int[7];
            var history = new string[7];

            for (var i = 0; i < 7; i++)
            {
                changes[i] = pastWeek[i].Active - pastWeek[i + 1].Active;

                var date = GetDate(pastWeek[i].UnixTimestamp);

                history[i] = $"{date:MMM dd} - *{pastWeek[i].Active} ({(changes[0] >= 0 ? "+" : "")}{changes[i]})*";
            }

            async Task<string[]> GetColumn(string zoneName, int zoneNum)
            {
                var zoneData = (await QueryData<DailyCityCovidInfo>("HealthZones", $"HealthZone='{zoneNum}'"))?.First();

                if (zoneData == null)
                {
                    return new[] { zoneName };
                }

                return new[]
                {
                        zoneName,
                        zoneData.ActiveCases.ToString(),
                        zoneData.NewToday.ToString(),
                        zoneData.RecoveryPhase
                    };
            }

            var columns = new List<string[]>
                {
                    new[]
                    {
                        "Zone:",
                        "Active Cases:",
                        "New:",
                        "Recovery Phase:",
                        "Recovered:"
                    },
                    new[]
                    {
                        "Overall",
                        province.ActiveCases.ToString(),
                        province.NewToday.ToString(),
                        string.IsNullOrWhiteSpace(province.RecoveryPhase) ? "Mixed" : province.RecoveryPhase,
                        pastWeek[0].NewRecoveredToday.ToString()
                    }
                };

            var zones = new (string zoneName, int zoneNum)[]
            {
                    ("Fredericton", 3),
                    ("Moncton", 1),
                    ("Edmundston", 4)
            };

            foreach (var (zoneName, zoneNum) in zones)
            {
                columns.Add(await GetColumn(zoneName, zoneNum));
            }

            var content = JoinColumns(2, columns.ToArray());

            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle("New Brunswick COVID-19 Statistics")
                .WithUrl("https://experience.arcgis.com/experience/8eeb9a2052d641c996dba5de8f25a8aa")
                .AddField("** **", $"```{content}```")
                .WithFooter("Bot by Stephen White - https://silk.one/", "https://static.silk.one/avatar.png")
                .WithCurrentTimestamp();

            await ReplyAsync(embed: embedBuilder.Build());
        }
    }
}
