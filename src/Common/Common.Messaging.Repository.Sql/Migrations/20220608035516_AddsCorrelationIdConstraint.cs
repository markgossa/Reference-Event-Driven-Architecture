using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Messaging.Outbox.Sql.Migrations
{
    public partial class AddsCorrelationIdConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "Messages",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CorrelationId",
                table: "Messages",
                column: "CorrelationId",
                unique: true,
                filter: "[CorrelationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_CorrelationId",
                table: "Messages");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
