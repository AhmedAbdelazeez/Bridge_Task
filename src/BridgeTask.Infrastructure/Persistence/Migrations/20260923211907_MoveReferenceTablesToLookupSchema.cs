using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgeTask.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveReferenceTablesToLookupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lookup");

            migrationBuilder.RenameTable(
                name: "Countries",
                newName: "Countries",
                newSchema: "lookup");

            migrationBuilder.RenameTable(
                name: "Cities",
                newName: "Cities",
                newSchema: "lookup");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Countries",
                schema: "lookup",
                newName: "Countries");

            migrationBuilder.RenameTable(
                name: "Cities",
                schema: "lookup",
                newName: "Cities");
        }
    }
}
