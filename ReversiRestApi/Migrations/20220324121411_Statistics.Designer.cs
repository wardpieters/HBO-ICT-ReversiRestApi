﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReversiRestApi.DAL;

namespace ReversiRestApi.Migrations
{
    [DbContext(typeof(ReversiContext))]
    [Migration("20220324121411_Statistics")]
    partial class Statistics
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.14")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ReversiRestApi.Model.Spel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AandeBeurt")
                        .HasColumnType("int");

                    b.Property<string>("Bord")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("GameFinished")
                        .HasColumnType("bit");

                    b.Property<int?>("GameWinner")
                        .HasColumnType("int");

                    b.Property<string>("GameWinnerPlayerToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Player1Token")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Player2Token")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Token")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Spellen");
                });
#pragma warning restore 612, 618
        }
    }
}
