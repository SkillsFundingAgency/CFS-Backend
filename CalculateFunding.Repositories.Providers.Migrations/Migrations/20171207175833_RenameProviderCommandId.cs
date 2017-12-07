using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Repositories.Providers.Migrations.Migrations
{
    public partial class RenameProviderCommandId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandEntityId",
                table: "ProviderCommandCandidates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderEvents_ProviderCommands_ProviderCommandEntityId",
                table: "ProviderEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProviderEvents_ProviderCommandEntityId",
                table: "ProviderEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProviderCommandCandidates_ProviderCommandEntityId",
                table: "ProviderCommandCandidates");

            migrationBuilder.DropColumn(
                name: "ProviderCommandEntityId",
                table: "ProviderEvents");

            migrationBuilder.DropColumn(
                name: "ProviderCommandEntityId",
                table: "ProviderCommandCandidates");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandId",
                table: "ProviderCommandCandidates",
                column: "ProviderCommandId",
                principalTable: "ProviderCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderEvents_ProviderCommands_ProviderCommandId",
                table: "ProviderEvents",
                column: "ProviderCommandId",
                principalTable: "ProviderCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandId",
                table: "ProviderCommandCandidates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderEvents_ProviderCommands_ProviderCommandId",
                table: "ProviderEvents");

            migrationBuilder.AddColumn<long>(
                name: "ProviderCommandEntityId",
                table: "ProviderEvents",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProviderCommandEntityId",
                table: "ProviderCommandCandidates",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvents_ProviderCommandEntityId",
                table: "ProviderEvents",
                column: "ProviderCommandEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCommandCandidates_ProviderCommandEntityId",
                table: "ProviderCommandCandidates",
                column: "ProviderCommandEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderCommandCandidates_ProviderCommands_ProviderCommandEntityId",
                table: "ProviderCommandCandidates",
                column: "ProviderCommandEntityId",
                principalTable: "ProviderCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderEvents_ProviderCommands_ProviderCommandEntityId",
                table: "ProviderEvents",
                column: "ProviderCommandEntityId",
                principalTable: "ProviderCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
