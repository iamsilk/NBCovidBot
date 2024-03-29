﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NBCovidBot.Covid;

namespace NBCovidBot.Migrations.CovidDataDb
{
    [DbContext(typeof(CovidDataDbContext))]
    [Migration("20220115182834_AddRapidTestsAndAlertLevels")]
    partial class AddRapidTestsAndAlertLevels
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("NBCovidBot.Covid.Models.ZoneDailyInfo", b =>
                {
                    b.Property<int>("ZoneNumber")
                        .HasColumnType("int");

                    b.Property<long>("LastUpdate")
                        .HasColumnType("bigint");

                    b.Property<int>("ActiveCases")
                        .HasColumnType("int");

                    b.Property<int>("Deaths")
                        .HasColumnType("int");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("NewToday")
                        .HasColumnType("int");

                    b.Property<int>("Recovered")
                        .HasColumnType("int");

                    b.Property<int>("TotalCases")
                        .HasColumnType("int");

                    b.Property<string>("ZoneTitle")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("ZoneNumber", "LastUpdate");

                    b.ToTable("ZoneData");

                    b.HasDiscriminator<string>("Discriminator").HasValue("ZoneDailyInfo");
                });

            modelBuilder.Entity("NBCovidBot.Covid.Models.ProvinceDailyInfo", b =>
                {
                    b.HasBaseType("NBCovidBot.Covid.Models.ZoneDailyInfo");

                    b.Property<int>("CloseContact")
                        .HasColumnType("int");

                    b.Property<int>("CommTransmission")
                        .HasColumnType("int");

                    b.Property<int>("Hospitalized")
                        .HasColumnType("int");

                    b.Property<int>("ICU")
                        .HasColumnType("int");

                    b.Property<int>("NewRapidTestPositives")
                        .HasColumnType("int");

                    b.Property<int>("TotalRapidTestPositives")
                        .HasColumnType("int");

                    b.Property<int>("TotalTests")
                        .HasColumnType("int");

                    b.Property<int>("TravelRelated")
                        .HasColumnType("int");

                    b.Property<int>("UnderInvestigation")
                        .HasColumnType("int");

                    b.HasDiscriminator().HasValue("ProvinceDailyInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
