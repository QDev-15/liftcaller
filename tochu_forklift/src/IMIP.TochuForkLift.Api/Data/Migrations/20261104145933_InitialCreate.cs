using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMIP.TochuForkLift.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiftCall",
                columns: table => new
                {
                    UnitNo = table.Column<int>(type: "int", nullable: false),
                    CallType = table.Column<byte>(type: "tinyint", nullable: false),
                    Palette = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    CalledAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSDATETIME()"),
                    IsResponding = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    RespondedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiftCall", x => x.UnitNo);
                    table.CheckConstraint("CK_LiftCall_CallType", "[CallType] IN (1,2,3)");
                    table.CheckConstraint("CK_LiftCall_Palette", "[Palette] BETWEEN 0 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "LiftCallHistory",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitNo = table.Column<int>(type: "int", nullable: false),
                    CallType = table.Column<byte>(type: "tinyint", nullable: false),
                    Palette = table.Column<byte>(type: "tinyint", nullable: false),
                    CalledAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CompletedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true, computedColumnSql: "DATEDIFF(SECOND, [CalledAt], [CompletedAt])", stored: true),
                    ResponseWaitSeconds = table.Column<int>(type: "int", nullable: true, computedColumnSql: "DATEDIFF(SECOND, [CalledAt], [RespondedAt])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiftCallHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "LiftState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiftState", x => x.Id);
                    table.CheckConstraint("CK_LiftState_SingleRow", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "Terminal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    UnitNo = table.Column<int>(type: "int", nullable: true),
                    PinHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminal", x => x.Id);
                    table.CheckConstraint("CK_Terminal_Role", "[Role] IN (1, 2)");
                    table.CheckConstraint("CK_Terminal_UnitNo_RequiredForCaller", "[Role] = 2 OR [UnitNo] IS NOT NULL");
                });

            migrationBuilder.InsertData(
                table: "LiftState",
                columns: new[] { "Id", "IsRunning", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.CreateIndex(
                name: "IX_LiftCall_CalledAt",
                table: "LiftCall",
                columns: new[] { "CalledAt", "UnitNo" });

            migrationBuilder.CreateIndex(
                name: "IX_LiftCallHistory_CompletedAt",
                table: "LiftCallHistory",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LiftCallHistory_UnitNo",
                table: "LiftCallHistory",
                columns: new[] { "UnitNo", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_Terminal_Name",
                table: "Terminal",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Terminal_UnitNo_Active",
                table: "Terminal",
                column: "UnitNo",
                unique: true,
                filter: "[Role] = 1 AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiftCall");

            migrationBuilder.DropTable(
                name: "LiftCallHistory");

            migrationBuilder.DropTable(
                name: "LiftState");

            migrationBuilder.DropTable(
                name: "Terminal");
        }
    }
}
