using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSA.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProgressTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_AspNetUsers_UserId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Lessons_LessonId",
                table: "UserProgresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProgresses",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses");

            migrationBuilder.RenameTable(
                name: "UserProgresses",
                newName: "UserProgress");

            migrationBuilder.RenameIndex(
                name: "IX_UserProgresses_LessonId",
                table: "UserProgress",
                newName: "IX_UserProgress_LessonId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAt",
                table: "UserProgress",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserProgress",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "UserProgress",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LessonId1",
                table: "UserProgress",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProgress",
                table: "UserProgress",
                columns: new[] { "UserId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProgress_LessonId1",
                table: "UserProgress",
                column: "LessonId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgress_AspNetUsers_UserId",
                table: "UserProgress",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgress_Lessons_LessonId",
                table: "UserProgress",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgress_Lessons_LessonId1",
                table: "UserProgress",
                column: "LessonId1",
                principalTable: "Lessons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgress_AspNetUsers_UserId",
                table: "UserProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgress_Lessons_LessonId",
                table: "UserProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgress_Lessons_LessonId1",
                table: "UserProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProgress",
                table: "UserProgress");

            migrationBuilder.DropIndex(
                name: "IX_UserProgress_LessonId1",
                table: "UserProgress");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "UserProgress");

            migrationBuilder.DropColumn(
                name: "LessonId1",
                table: "UserProgress");

            migrationBuilder.RenameTable(
                name: "UserProgress",
                newName: "UserProgresses");

            migrationBuilder.RenameIndex(
                name: "IX_UserProgress_LessonId",
                table: "UserProgresses",
                newName: "IX_UserProgresses_LessonId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAt",
                table: "UserProgresses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserProgresses",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProgresses",
                table: "UserProgresses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_AspNetUsers_UserId",
                table: "UserProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Lessons_LessonId",
                table: "UserProgresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
