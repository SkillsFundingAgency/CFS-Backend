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
                name: "Reference",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    URN = table.Column<string>(nullable: false),
                    Address3 = table.Column<string>(nullable: true),
                    AdministrativeWardId = table.Column<string>(nullable: true),
                    AdmissionsPolicyId = table.Column<string>(nullable: true),
                    AuthorityId = table.Column<string>(nullable: true),
                    BoardersId = table.Column<string>(nullable: true),
                    CCF = table.Column<string>(nullable: true),
                    CensusAreaStatisticWard = table.Column<string>(nullable: true),
                    CensusDate = table.Column<DateTimeOffset>(nullable: true),
                    CloseDate = table.Column<DateTimeOffset>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    County = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    DioceseId = table.Column<string>(nullable: true),
                    DistrictAdministrativeId = table.Column<string>(nullable: true),
                    EBD = table.Column<string>(nullable: true),
                    Easting = table.Column<int>(nullable: true),
                    EdByOther = table.Column<string>(nullable: true),
                    EstablishmentName = table.Column<string>(nullable: true),
                    EstablishmentNumber = table.Column<string>(nullable: true),
                    EstablishmentStatusId = table.Column<string>(nullable: true),
                    EstablishmentTypeGroupId = table.Column<string>(nullable: true),
                    EstablishmentTypeId = table.Column<string>(nullable: true),
                    FEHEIdentifier = table.Column<string>(nullable: true),
                    FTProv = table.Column<string>(nullable: true),
                    FederationFlag = table.Column<string>(nullable: true),
                    FederationsId = table.Column<string>(nullable: true),
                    FurtherEducationType = table.Column<string>(nullable: true),
                    GORId = table.Column<string>(nullable: true),
                    GSSLACode = table.Column<string>(nullable: true),
                    GenderId = table.Column<string>(nullable: true),
                    LSOA = table.Column<string>(nullable: true),
                    LastChangedDate = table.Column<DateTimeOffset>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    MSOA = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Northing = table.Column<int>(nullable: true),
                    NumberOfBoys = table.Column<int>(nullable: true),
                    NumberOfGirls = table.Column<int>(nullable: true),
                    NumberOfPupils = table.Column<int>(nullable: true),
                    NurseryProvisionId = table.Column<string>(nullable: true),
                    OfficialSixthFormId = table.Column<string>(nullable: true),
                    OfstedLastInspectionDate = table.Column<DateTimeOffset>(nullable: true),
                    OfstedRating = table.Column<string>(nullable: true),
                    OfstedSpecialMeasuresId = table.Column<string>(nullable: true),
                    OpenDate = table.Column<DateTimeOffset>(nullable: true),
                    PRUPlaces = table.Column<int>(nullable: true),
                    ParliamentaryConstituencyId = table.Column<string>(nullable: true),
                    PercentageFSM = table.Column<decimal>(nullable: true),
                    PhaseOfEducationId = table.Column<string>(nullable: true),
                    Postcode = table.Column<string>(nullable: true),
                    RSCRegion = table.Column<string>(nullable: true),
                    ReasonEstablishmentClosedId = table.Column<string>(nullable: true),
                    ReasonEstablishmentOpenedId = table.Column<string>(nullable: true),
                    ReligiousCharacterId = table.Column<string>(nullable: true),
                    ReligiousEthosId = table.Column<string>(nullable: true),
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
                    SpecialClassesId = table.Column<string>(nullable: true),
                    StatutoryHighAge = table.Column<int>(nullable: true),
                    StatutoryLowAge = table.Column<int>(nullable: true),
                    Street = table.Column<string>(nullable: true),
                    TeenMoth = table.Column<string>(nullable: true),
                    TeenMothPlaces = table.Column<int>(nullable: true),
                    Telephone = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Town = table.Column<string>(nullable: true),
                    TrustSchoolFlagId = table.Column<string>(nullable: true),
                    TrustsId = table.Column<string>(nullable: true),
                    TypeOfResourcedProvision = table.Column<string>(nullable: true),
                    UKPRN = table.Column<string>(nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UrbanRuralId = table.Column<string>(nullable: true),
                    Website = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.URN);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_AdministrativeWardId",
                        column: x => x.AdministrativeWardId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_AdmissionsPolicyId",
                        column: x => x.AdmissionsPolicyId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_AuthorityId",
                        column: x => x.AuthorityId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_BoardersId",
                        column: x => x.BoardersId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_DioceseId",
                        column: x => x.DioceseId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_DistrictAdministrativeId",
                        column: x => x.DistrictAdministrativeId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_EstablishmentStatusId",
                        column: x => x.EstablishmentStatusId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_EstablishmentTypeGroupId",
                        column: x => x.EstablishmentTypeGroupId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_EstablishmentTypeId",
                        column: x => x.EstablishmentTypeId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_FederationsId",
                        column: x => x.FederationsId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_GORId",
                        column: x => x.GORId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_NurseryProvisionId",
                        column: x => x.NurseryProvisionId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_OfficialSixthFormId",
                        column: x => x.OfficialSixthFormId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_OfstedSpecialMeasuresId",
                        column: x => x.OfstedSpecialMeasuresId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_ParliamentaryConstituencyId",
                        column: x => x.ParliamentaryConstituencyId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_PhaseOfEducationId",
                        column: x => x.PhaseOfEducationId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_ReasonEstablishmentClosedId",
                        column: x => x.ReasonEstablishmentClosedId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_ReasonEstablishmentOpenedId",
                        column: x => x.ReasonEstablishmentOpenedId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_ReligiousCharacterId",
                        column: x => x.ReligiousCharacterId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_ReligiousEthosId",
                        column: x => x.ReligiousEthosId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_SpecialClassesId",
                        column: x => x.SpecialClassesId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_TrustSchoolFlagId",
                        column: x => x.TrustSchoolFlagId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_TrustsId",
                        column: x => x.TrustsId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Reference_UrbanRuralId",
                        column: x => x.UrbanRuralId,
                        principalTable: "Reference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_AdministrativeWardId",
                table: "Providers",
                column: "AdministrativeWardId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_AdmissionsPolicyId",
                table: "Providers",
                column: "AdmissionsPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_AuthorityId",
                table: "Providers",
                column: "AuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_BoardersId",
                table: "Providers",
                column: "BoardersId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_DioceseId",
                table: "Providers",
                column: "DioceseId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_DistrictAdministrativeId",
                table: "Providers",
                column: "DistrictAdministrativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_EstablishmentStatusId",
                table: "Providers",
                column: "EstablishmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_EstablishmentTypeGroupId",
                table: "Providers",
                column: "EstablishmentTypeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_EstablishmentTypeId",
                table: "Providers",
                column: "EstablishmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_FederationsId",
                table: "Providers",
                column: "FederationsId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_GORId",
                table: "Providers",
                column: "GORId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_GenderId",
                table: "Providers",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_NurseryProvisionId",
                table: "Providers",
                column: "NurseryProvisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_OfficialSixthFormId",
                table: "Providers",
                column: "OfficialSixthFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_OfstedSpecialMeasuresId",
                table: "Providers",
                column: "OfstedSpecialMeasuresId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ParliamentaryConstituencyId",
                table: "Providers",
                column: "ParliamentaryConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_PhaseOfEducationId",
                table: "Providers",
                column: "PhaseOfEducationId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ReasonEstablishmentClosedId",
                table: "Providers",
                column: "ReasonEstablishmentClosedId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ReasonEstablishmentOpenedId",
                table: "Providers",
                column: "ReasonEstablishmentOpenedId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ReligiousCharacterId",
                table: "Providers",
                column: "ReligiousCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ReligiousEthosId",
                table: "Providers",
                column: "ReligiousEthosId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_SpecialClassesId",
                table: "Providers",
                column: "SpecialClassesId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TrustSchoolFlagId",
                table: "Providers",
                column: "TrustSchoolFlagId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TrustsId",
                table: "Providers",
                column: "TrustsId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_UrbanRuralId",
                table: "Providers",
                column: "UrbanRuralId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Reference");
        }
    }
}
