using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMIP.TochuForkLift.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LiftStatePerTerminal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LiftState_SingleRow",
                table: "LiftState");

            migrationBuilder.DeleteData(
                table: "LiftState",
                keyColumn: "Id",
                keyColumnType: "int",
                keyValue: 1);

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LiftState",
                newName: "TerminalId");

            migrationBuilder.AddForeignKey(
                name: "FK_LiftState_Terminal_TerminalId",
                table: "LiftState",
                column: "TerminalId",
                principalTable: "Terminal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LiftState_Terminal_TerminalId",
                table: "LiftState");

            migrationBuilder.RenameColumn(
                name: "TerminalId",
                table: "LiftState",
                newName: "Id");

            migrationBuilder.InsertData(
                table: "LiftState",
                columns: new[] { "Id", "IsRunning", "UpdatedAt", "UpdatedBy" },
                columnTypes: new[] { "int", "bit", "datetime2(3)", "nvarchar(50)" },
                values: new object[] { 1, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LiftState_SingleRow",
                table: "LiftState",
                sql: "[Id] = 1");
        }
    }
}
