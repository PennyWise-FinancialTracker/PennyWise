using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PennyWise.Migrations
{
    /// <inheritdoc />
    public partial class RecurringFrequency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastGeneratedMonth",
                table: "RecurringTransactions");

            migrationBuilder.RenameColumn(
                name: "LastGeneratedYear",
                table: "RecurringTransactions",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "DayOfMonth",
                table: "RecurringTransactions",
                newName: "Frequency");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeneratedDate",
                table: "RecurringTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "RecurringTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_AccountId",
                table: "RecurringTransactions",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Accounts_AccountId",
                table: "RecurringTransactions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Accounts_AccountId",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_AccountId",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "LastGeneratedDate",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "RecurringTransactions");

            migrationBuilder.RenameColumn(
                name: "Frequency",
                table: "RecurringTransactions",
                newName: "DayOfMonth");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "RecurringTransactions",
                newName: "LastGeneratedYear");

            migrationBuilder.AddColumn<int>(
                name: "LastGeneratedMonth",
                table: "RecurringTransactions",
                type: "INTEGER",
                nullable: true);
        }
    }
}
