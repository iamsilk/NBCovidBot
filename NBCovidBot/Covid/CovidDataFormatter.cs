﻿using Discord;
using Microsoft.Extensions.Configuration;
using NBCovidBot.Covid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBCovidBot.Covid
{
    public class CovidDataFormatter
    {
        private readonly CovidDataProvider _dataProvider;
        private readonly IConfiguration _configuration;

        public CovidDataFormatter(CovidDataProvider dataProvider,
            IConfiguration configuration)
        {
            _dataProvider = dataProvider;
            _configuration = configuration;
        }

        private DateTimeOffset GetDateTime()
        {
            //var timeZone = TZConvert.GetTimeZoneInfo(_configuration["TimeZone"]);
            return DateTime.UtcNow; //TimeZoneInfo.ConvertTime(DateTime.Now, timeZone);
        }

        private static string JoinRows(int spacing, params string[][] rows)
        {
            var numCols = rows.Max(x => x.Length);

            var maxColLens = new int[numCols];

            foreach (var row in rows)
            {
                for (var j = 0; j < numCols; j++)
                {
                    if (row.Length <= j)
                        break;

                    if (row[j].Length > maxColLens[j])
                        maxColLens[j] = row[j].Length;
                }
            }

            var builder = new StringBuilder();

            foreach (var row in rows)
            {
                for (var j = 0; j < numCols; j++)
                {
                    var spaces = maxColLens[j] + spacing;

                    if (row.Length > j)
                    {
                        builder.Append(row[j]);
                        spaces -= row[j].Length;
                    }

                    builder.Append(new string(' ', spaces));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public Embed GetEmbed()
        {
            var zonesDailyInfo = _dataProvider.GetZonesDailyInfo()
                ?.OrderByDescending(x => x.ActiveCases).ToList();

            var provinceDailyInfo = _dataProvider.GetProvinceDailyInfo();

            var provincePastWeek = _dataProvider.GetProvincePastInfo()
                ?.OrderByDescending(x => x.UnixTimestamp).Take(7).ToList();

            var provinceVaccineInfo = _dataProvider.GetProvinceVaccineInfo();

            var provinceHospitalTrendsInfo = _dataProvider.GetProvinceHospitalTrendsInfo();

            if (zonesDailyInfo == null || zonesDailyInfo.Count == 0 || provinceDailyInfo == null ||
                provincePastWeek == null || provincePastWeek.Count == 0 || provinceVaccineInfo == null ||
                provinceHospitalTrendsInfo == null)
            {
                return null;
            }

            var zonesRecoveryPhases = new List<ZoneRecoveryPhaseInfo>();

            foreach (var recoveryPhaseInfo in _dataProvider.GetZonesRecoveryPhaseInfo())
            {
                if (zonesDailyInfo.Any(x =>
                    x.ZoneTitle.Equals(recoveryPhaseInfo.CommunityName, StringComparison.OrdinalIgnoreCase)))
                {
                    zonesRecoveryPhases.Add(recoveryPhaseInfo);
                }
            }

            const int extraRowCount = 3;
            var rows = new string[zonesDailyInfo.Count + extraRowCount][];

            var i = 0;

            rows[i++] = new[]
            {
                "Zone:",
                "Active",
                "New",
                "Alert"
            };

            rows[i++] = new[]
            {
                "",
                "Cases:",
                "Cases:",
                "Level:"
            };

            rows[i++] = new[]
            {
                "Overall",
                provinceDailyInfo.ActiveCases.ToString(),
                provinceDailyInfo.NewToday.ToString()
            };

            for (; i < rows.Length; i++)
            {
                var zone = zonesDailyInfo[i - extraRowCount];

                var recoveryPhase = zonesRecoveryPhases.FirstOrDefault(x => x.HealthZone == zone.ZoneNumber);

                rows[i] = new[]
                {
                    zone.ZoneTitle,
                    zone.ActiveCases.ToString(),
                    zone.NewToday.ToString(),
                    recoveryPhase == null ? "Unknown" : recoveryPhase.AlertLevel
                };
            }

            var briefZoneContent = JoinRows(2, rows);

            var verboseProvinceContent = JoinRows(2,
                new[] { "Total Cases:", provinceDailyInfo.TotalCases.ToString() },
                new[] { "Travel Related:", provinceDailyInfo.TravelRelated.ToString() },
                new[] { "Close Contact:", provinceDailyInfo.CloseContact.ToString() },
                new[] { "Community Transmission:", provinceDailyInfo.CommTransmission.ToString() },
                new[] { "Under Investigation:", provinceDailyInfo.UnderInvestigation.ToString() },
                new[] { "Total Rapid Test Positives:", $"{provinceDailyInfo.TotalRapidTestPositives} (+{provinceDailyInfo.NewRapidTestPositives})" },
                new[] { "Current Hospitalized:", provinceHospitalTrendsInfo.CurrentHospitalizations.ToString() },
                new[] { "Current ICU:", provinceHospitalTrendsInfo.CurrentICU.ToString() });

            var vaccineContent = JoinRows(2,
                new[] {"Total Doses Administered:", provinceVaccineInfo.TotalAdministered.ToString()},
                new[]
                {
                    "Pop. With First Dose:",
                    $"{provinceVaccineInfo.PercentOneDose:0.##}% ({provinceVaccineInfo.PopulationOneDose})"
                },
                new[]
                {
                    "Pop. With Second Dose:",
                    $"{provinceVaccineInfo.PercentSecondDose:0.##}% ({provinceVaccineInfo.PopulationSecondDose})"
                },
                new[]
                {
                    "Estimated Fully Vaccinated:",
                    (provinceVaccineInfo.TotalAdministered - provinceVaccineInfo.PopulationOneDose).ToString()
                });
            
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle("New Brunswick COVID-19 Statistics")
                .WithUrl("https://experience.arcgis.com/experience/8eeb9a2052d641c996dba5de8f25a8aa")
                .WithColor(Color.Green)
                .AddField("Brief Data per Zone:", $"```{briefZoneContent}```")
                .AddField("Provincial Information:", $"```{verboseProvinceContent}```")
                .AddField("Provincial Vaccine Information:", $"```{vaccineContent}```")
                .WithFooter("Bot by Stephen White - https://silk.one/\n" +
                            "Check out my code: https://github.com/IAmSilK/NBCovidBot/\n" +
                            _configuration["UserUpdates:Reactions:Subscribe"] + "- Subscribe to notifications\n" +
                            _configuration["UserUpdates:Reactions:Unsubscribe"] + "- Unsubscribe from notifications")
                .WithTimestamp(GetDateTime());

            return embedBuilder.Build();
        }

        public async Task AddReactions(IUserMessage message)
        {
            var emotes = new IEmote[]
            {
                new Emoji(_configuration["UserUpdates:Reactions:Subscribe"]),
                new Emoji(_configuration["UserUpdates:Reactions:Unsubscribe"])
            };

            await message.AddReactionsAsync(emotes);
        }
    }
}
