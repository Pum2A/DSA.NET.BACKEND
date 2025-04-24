using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSA.Migrations
{
    /// <inheritdoc />
    public partial class addedJoinedAtInAuthService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Users SET JoinedAt = datetime('now') WHERE JoinedAt = '0001-01-01 00:00:00';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
