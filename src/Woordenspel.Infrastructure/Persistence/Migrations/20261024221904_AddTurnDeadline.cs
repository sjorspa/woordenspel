using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woordenspel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TurnDeadline",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status_TurnDeadline",
                table: "Games",
                columns: new[] { "Status", "TurnDeadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Status_TurnDeadline",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TurnDeadline",
                table: "Games");
        }
    }
}
