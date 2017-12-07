using Microsoft.EntityFrameworkCore.Metadata;
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
                name: "ProviderCommands",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    ProviderURN = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderCommands_Providers_ProviderURN",
                        column: x => x.ProviderURN,
                        principalTable: "Providers",
                        principalColumn: "URN",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCommandCandidates",
                columns: table => new
                {
                    ProviderCommandId = table.Column<long>(nullable: false),
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
                    ProviderCommandEntityId = table.Column<long>(nullable: true),
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
                        name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandEntityId",
                        column: x => x.ProviderCommandEntityId,
                        principalTable: "ProviderCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderCommandCandidates_Providers_URN",
                        column: x => x.URN,
                        principalTable: "Providers",
                        principalColumn: "URN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderEvents",
                columns: table => new
                {
                    ProviderCommandId = table.Column<long>(nullable: false),
                    URN = table.Column<string>(nullable: false),
                    Action = table.Column<string>(nullable: false),
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
                    ProviderCommandEntityId = table.Column<long>(nullable: true),
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
                    table.PrimaryKey("PK_ProviderEvents", x => new { x.ProviderCommandId, x.URN });
                    table.ForeignKey(
                        name: "FK_ProviderEvents_ProviderCommands_ProviderCommandEntityId",
                        column: x => x.ProviderCommandEntityId,
                        principalTable: "ProviderCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderEvents_Providers_URN",
                        column: x => x.URN,
                        principalTable: "Providers",
                        principalColumn: "URN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommandCandidates_ProviderCommandEntityId",
                table: "ProviderCommandCandidates",
                column: "ProviderCommandEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommandCandidates_URN",
                table: "ProviderCommandCandidates",
                column: "URN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommandCandidates_ProviderCommandId_URN",
                table: "ProviderCommandCandidates",
                columns: new[] { "ProviderCommandId", "URN" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommands_ProviderURN",
                table: "ProviderCommands",
                column: "ProviderURN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_ProviderCommandEntityId",
                table: "ProviderEvents",
                column: "ProviderCommandEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_URN",
                table: "ProviderEvents",
                column: "URN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_ProviderCommandId_URN",
                table: "ProviderEvents",
                columns: new[] { "ProviderCommandId", "URN" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_URN",
                table: "Providers",
                column: "URN");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderCommandCandidates");

            migrationBuilder.DropTable(
                name: "ProviderEvents");

            migrationBuilder.DropTable(
                name: "ProviderCommands");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
