using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MembershipAppBEAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberFollowUpTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Households",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Households",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Households",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Households",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadMemberId",
                table: "Households",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadMemberId1",
                table: "Households",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Households",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Households",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryPhone",
                table: "Households",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MemberId1",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberFollowUps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    FollowedById = table.Column<int>(type: "int", nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Outcome = table.Column<int>(type: "int", maxLength: 100, nullable: true),
                    RequiresFurtherFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    NextFollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFollowUps_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberFollowUps_Users_FollowedById",
                        column: x => x.FollowedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChurchName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChurchAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChurchPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChurchEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_WelfareMemberId",
                table: "Members",
                column: "WelfareMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_HeadMemberId1",
                table: "Households",
                column: "HeadMemberId1");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_MemberId1",
                table: "Attendance",
                column: "MemberId1");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFollowUps_FollowedById",
                table: "MemberFollowUps",
                column: "FollowedById");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFollowUps_MemberId",
                table: "MemberFollowUps",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Members_MemberId1",
                table: "Attendance",
                column: "MemberId1",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Households_Members_HeadMemberId1",
                table: "Households",
                column: "HeadMemberId1",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Members_WelfareMemberId",
                table: "Members",
                column: "WelfareMemberId",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Members_MemberId1",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Households_Members_HeadMemberId1",
                table: "Households");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Members_WelfareMemberId",
                table: "Members");

            migrationBuilder.DropTable(
                name: "MemberFollowUps");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Members_WelfareMemberId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Households_HeadMemberId1",
                table: "Households");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_MemberId1",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "HeadMemberId",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "HeadMemberId1",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "PrimaryPhone",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "Attendance");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Households",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Households",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
