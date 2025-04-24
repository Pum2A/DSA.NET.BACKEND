using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSA.Migrations
{
    /// <inheritdoc />
    public partial class addedUtcToHaveAcutallyTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET JoinedAt = CURRENT_TIMESTAMP WHERE JoinedAt = '0001-01-01T00:00:00';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
