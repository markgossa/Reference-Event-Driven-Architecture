using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Messaging.Outbox.Sql.Migrations
{
    public partial class AddCompletedOnField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOn",
                table: "Messages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "Messages");
        }
    }
}
