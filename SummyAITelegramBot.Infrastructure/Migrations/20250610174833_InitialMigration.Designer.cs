﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SummyAITelegramBot.Infrastructure.Context;

#nullable disable

namespace SummyAITelegramBot.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250610174833_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ChannelUser", b =>
                {
                    b.Property<long>("ChannelsId")
                        .HasColumnType("bigint");

                    b.Property<long>("UsersId")
                        .HasColumnType("bigint");

                    b.HasKey("ChannelsId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("ChannelUser");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.Channel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool>("HasStopFactor")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("boolean");

                    b.Property<string>("Link")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Channel");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.ChannelPost", b =>
                {
                    b.Property<long>("ChannelId")
                        .HasColumnType("bigint");

                    b.Property<int>("Id")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ChannelId", "Id");

                    b.ToTable("ChannelPost");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool?>("AddedToAttachmentMenu")
                        .HasColumnType("boolean");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<string>("FirstName")
                        .HasColumnType("text");

                    b.Property<bool>("IsBot")
                        .HasColumnType("boolean");

                    b.Property<bool?>("IsPremium")
                        .HasColumnType("boolean");

                    b.Property<string>("LanguageCode")
                        .HasColumnType("text");

                    b.Property<DateTime?>("LastInteractionAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastName")
                        .HasColumnType("text");

                    b.Property<float?>("Latitude")
                        .HasColumnType("real");

                    b.Property<float?>("Longitude")
                        .HasColumnType("real");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<long>("TelegramId")
                        .HasColumnType("bigint");

                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.UserSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AiModel")
                        .HasColumnType("integer");

                    b.Property<long>("ChannelId")
                        .HasColumnType("bigint");

                    b.Property<bool>("InstantlyNotification")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsBlockingSimilarPostsInChannels")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsGlobal")
                        .HasColumnType("boolean");

                    b.Property<int>("Language")
                        .HasColumnType("integer");

                    b.Property<bool>("MediaEnabled")
                        .HasColumnType("boolean");

                    b.Property<TimeOnly?>("NotificationTime")
                        .HasColumnType("time without time zone");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserId");

                    b.ToTable("UserSettings");
                });

            modelBuilder.Entity("ChannelUser", b =>
                {
                    b.HasOne("SummyAITelegramBot.Core.Domain.Models.Channel", null)
                        .WithMany()
                        .HasForeignKey("ChannelsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SummyAITelegramBot.Core.Domain.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.ChannelPost", b =>
                {
                    b.HasOne("SummyAITelegramBot.Core.Domain.Models.Channel", "Channel")
                        .WithMany("Posts")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.UserSettings", b =>
                {
                    b.HasOne("SummyAITelegramBot.Core.Domain.Models.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SummyAITelegramBot.Core.Domain.Models.User", "User")
                        .WithMany("UserSettings")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.Channel", b =>
                {
                    b.Navigation("Posts");
                });

            modelBuilder.Entity("SummyAITelegramBot.Core.Domain.Models.User", b =>
                {
                    b.Navigation("UserSettings");
                });
#pragma warning restore 612, 618
        }
    }
}
