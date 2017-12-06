using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Repositories.Providers.Migrations.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    URN = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ResourcedProvisionCapacity = table.Column<int>(nullable: true),
                    ResourcedProvisionOnRoll = table.Column<int>(nullable: true),
                    SEN1 = table.Column<string>(nullable: true),
                    SENNoStat = table.Column<int>(nullable: true),
                    SENPRU = table.Column<string>(nullable: true),
                    SENStat = table.Column<int>(nullable: true),
                    SchoolCapacity = table.Column<int>(nullable: true),
                    SchoolSponsorFlag = table.Column<string>(nullable: true),
                    SchoolSponsors = table.Column<string>(nullable: true),
                    Section41Approved = table.Column<string>(nullable: true),
                    SenUnitCapacity = table.Column<int>(nullable: true),
                    SenUnitOnRoll = table.Column<int>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    UKPRN = table.Column<string>(nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.URN);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCommandCandidates",
                columns: table => new
                {
                    ProviderCommandId = table.Column<Guid>(nullable: false),
                    URN = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ResourcedProvisionCapacity = table.Column<int>(nullable: true),
                    ResourcedProvisionOnRoll = table.Column<int>(nullable: true),
                    SEN1 = table.Column<string>(nullable: true),
                    SENNoStat = table.Column<int>(nullable: true),
                    SENPRU = table.Column<string>(nullable: true),
                    SENStat = table.Column<int>(nullable: true),
                    SchoolCapacity = table.Column<int>(nullable: true),
                    SchoolSponsorFlag = table.Column<string>(nullable: true),
                    SchoolSponsors = table.Column<string>(nullable: true),
                    Section41Approved = table.Column<string>(nullable: true),
                    SenUnitCapacity = table.Column<int>(nullable: true),
                    SenUnitOnRoll = table.Column<int>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    UKPRN = table.Column<string>(nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCommandCandidates", x => new { x.ProviderCommandId, x.URN });
                    table.ForeignKey(
                        name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandId",
                        column: x => x.ProviderCommandId,
                        principalTable: "ProviderCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderCommandCandidates");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "ProviderCommands");
        }
    }
}
