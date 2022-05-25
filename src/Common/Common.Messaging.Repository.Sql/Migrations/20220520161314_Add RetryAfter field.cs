using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Messaging.Outbox.Sql.Migrations
{
    public partial class AddRetryAfterfield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetryAfter",
                table: "Messages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryAfter",
                table: "Messages");
        }
    }
}
