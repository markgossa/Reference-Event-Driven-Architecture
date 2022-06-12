﻿// <auto-generated />
using System;
using Common.Messaging.Repository.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Common.Messaging.Outbox.Sql.Migrations
{
    [DbContext(typeof(MessageDbContext))]
    [Migration("20220608035516_AddsCorrelationIdConstraint")]
    partial class AddsCorrelationIdConstraint
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Common.Messaging.Repository.Sql.Models.MessageSqlRow", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("AttemptCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("CompletedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("CorrelationId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("LastAttempt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LockExpiry")
                        .HasColumnType("datetime2");

                    b.Property<string>("MessageBlob")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("RetryAfter")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("CorrelationId")
                        .IsUnique()
                        .HasFilter("[CorrelationId] IS NOT NULL");

                    b.ToTable("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
