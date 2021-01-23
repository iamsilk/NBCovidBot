using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
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
using Discord;
using TimeZoneConverter;

namespace StebumBot.Modules.Covid
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

        [Command("covid")]
        [Summary("View COVID stats")]
        public async Task CovidAsync()
        {
            var province = (await QueryData<DailyProvinceCovidInfo>("HealthZones", "HealthZone='Province'"))?.First();
            if (province == null) return;

            var moncton = (await QueryData<DailyCityCovidInfo>("HealthZones", "HealthZone='1'"))?.First();
            if (moncton == null) return;

            var fredericton = (await QueryData<DailyCityCovidInfo>("HealthZones", "HealthZone='3'"))?.First();
            if (fredericton == null) return;

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
            
            var embedBuilder = new EmbedBuilder();

            var totalFieldText =
                $"Active Cases: *{province.ActiveCases}*\n" +
                $"New: *{province.NewToday}*\n" +
                $"Phase: *{province.RecoveryPhase}*\n" +
                $"Recovered: *{pastWeek[0].NewRecoveredToday}*";
            
            var fredFieldText =
                $"*{fredericton.ActiveCases}*\n" +
                $"*{fredericton.NewToday}*\n" +
                $"*{fredericton.RecoveryPhase}*\n";

            var moncFieldText =
                $"*{moncton.ActiveCases}*\n" +
                $"*{moncton.NewToday}*\n" +
                $"*{moncton.RecoveryPhase}*\n";

            embedBuilder
                .WithTitle("New Brunswick COVID-19 Statistics")
                .WithUrl("https://experience.arcgis.com/experience/8eeb9a2052d641c996dba5de8f25a8aa")
                .AddField("Total", totalFieldText, true)
                .AddField("Fredericton", fredFieldText, true)
                .AddField("Moncton", moncFieldText, true)
                .WithCurrentTimestamp();

            await ReplyAsync(embed: embedBuilder.Build());

            /*
            var latestDate = GetDate(province.LastUpdate);

            var nl = Environment.NewLine;
            
            var response = $"__**{latestDate:MMMM dd, yyyy} (Provincial / Fredericton):**__" + nl +
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

            await ReplyAsync(response);*/
        }
    }
}
