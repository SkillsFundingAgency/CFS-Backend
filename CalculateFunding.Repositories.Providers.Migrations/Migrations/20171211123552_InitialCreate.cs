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
                    UKPRN = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    AdministrativeWard = table.Column<string>(nullable: true),
                    AdmissionsPolicy = table.Column<string>(nullable: true),
                    Authority = table.Column<string>(nullable: true),
                    Boarders = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Diocese = table.Column<string>(nullable: true),
                    DistrictAdministrative = table.Column<string>(nullable: true),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    EstablishmentStatus = table.Column<string>(nullable: true),
                    EstablishmentType = table.Column<string>(nullable: true),
                    EstablishmentTypeGroup = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    Federations = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GOR = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    NurseryProvision = table.Column<string>(nullable: true),
                    OfficialSixthForm = table.Column<string>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OfstedSpecialMeasures = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    ParliamentaryConstituency = table.Column<string>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    PhaseOfEducation = table.Column<string>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ReasonEstablishmentClosed = table.Column<string>(nullable: true),
                    ReasonEstablishmentOpened = table.Column<string>(nullable: true),
                    ReligiousCharacter = table.Column<string>(nullable: true),
                    ReligiousEthos = table.Column<string>(nullable: true),
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
                    SpecialClasses = table.Column<string>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TrustSchoolFlag = table.Column<string>(nullable: true),
                    Trusts = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    URN = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UrbanRural = table.Column<string>(nullable: true),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.UKPRN);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCommands",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    ProviderUKPRN = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderCommands_Providers_ProviderUKPRN",
                        column: x => x.ProviderUKPRN,
                        principalTable: "Providers",
                        principalColumn: "UKPRN",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCandidates",
                columns: table => new
                {
                    ProviderCommandId = table.Column<long>(nullable: false),
                    UKPRN = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    AdministrativeWard = table.Column<string>(nullable: true),
                    AdmissionsPolicy = table.Column<string>(nullable: true),
                    Authority = table.Column<string>(nullable: true),
                    Boarders = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Diocese = table.Column<string>(nullable: true),
                    DistrictAdministrative = table.Column<string>(nullable: true),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    EstablishmentStatus = table.Column<string>(nullable: true),
                    EstablishmentType = table.Column<string>(nullable: true),
                    EstablishmentTypeGroup = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    Federations = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GOR = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    NurseryProvision = table.Column<string>(nullable: true),
                    OfficialSixthForm = table.Column<string>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OfstedSpecialMeasures = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    ParliamentaryConstituency = table.Column<string>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    PhaseOfEducation = table.Column<string>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ReasonEstablishmentClosed = table.Column<string>(nullable: true),
                    ReasonEstablishmentOpened = table.Column<string>(nullable: true),
                    ReligiousCharacter = table.Column<string>(nullable: true),
                    ReligiousEthos = table.Column<string>(nullable: true),
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
                    SpecialClasses = table.Column<string>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TrustSchoolFlag = table.Column<string>(nullable: true),
                    Trusts = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    URN = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UrbanRural = table.Column<string>(nullable: true),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCandidates", x => new { x.ProviderCommandId, x.UKPRN });
                    table.ForeignKey(
                        name: "FK_ProviderCandidates_ProviderCommands_ProviderCommandId",
                        column: x => x.ProviderCommandId,
                        principalTable: "ProviderCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderCandidates_Providers_UKPRN",
                        column: x => x.UKPRN,
                        principalTable: "Providers",
                        principalColumn: "UKPRN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderEvents",
                columns: table => new
                {
                    ProviderCommandId = table.Column<long>(nullable: false),
                    UKPRN = table.Column<string>(nullable: false),
                    Action = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    AdministrativeWard = table.Column<string>(nullable: true),
                    AdmissionsPolicy = table.Column<string>(nullable: true),
                    Authority = table.Column<string>(nullable: true),
                    Boarders = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    Diocese = table.Column<string>(nullable: true),
                    DistrictAdministrative = table.Column<string>(nullable: true),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    EstablishmentStatus = table.Column<string>(nullable: true),
                    EstablishmentType = table.Column<string>(nullable: true),
                    EstablishmentTypeGroup = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    Federations = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GOR = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    NurseryProvision = table.Column<string>(nullable: true),
                    OfficialSixthForm = table.Column<string>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OfstedSpecialMeasures = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    ParliamentaryConstituency = table.Column<string>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    PhaseOfEducation = table.Column<string>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ReasonEstablishmentClosed = table.Column<string>(nullable: true),
                    ReasonEstablishmentOpened = table.Column<string>(nullable: true),
                    ReligiousCharacter = table.Column<string>(nullable: true),
                    ReligiousEthos = table.Column<string>(nullable: true),
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
                    SpecialClasses = table.Column<string>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TrustSchoolFlag = table.Column<string>(nullable: true),
                    Trusts = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    URN = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UrbanRural = table.Column<string>(nullable: true),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderEvents", x => new { x.ProviderCommandId, x.UKPRN });
                    table.ForeignKey(
                        name: "FK_ProviderEvents_ProviderCommands_ProviderCommandId",
                        column: x => x.ProviderCommandId,
                        principalTable: "ProviderCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderEvents_Providers_UKPRN",
                        column: x => x.UKPRN,
                        principalTable: "Providers",
                        principalColumn: "UKPRN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCandidates_UKPRN",
                table: "ProviderCandidates",
                column: "UKPRN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCandidates_ProviderCommandId_UKPRN",
                table: "ProviderCandidates",
                columns: new[] { "ProviderCommandId", "UKPRN" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommands_ProviderUKPRN",
                table: "ProviderCommands",
                column: "ProviderUKPRN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_UKPRN",
                table: "ProviderEvents",
                column: "UKPRN");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_ProviderCommandId_UKPRN",
                table: "ProviderEvents",
                columns: new[] { "ProviderCommandId", "UKPRN" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_UKPRN",
                table: "Providers",
                column: "UKPRN");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderCandidates");

            migrationBuilder.DropTable(
                name: "ProviderEvents");

            migrationBuilder.DropTable(
                name: "ProviderCommands");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
