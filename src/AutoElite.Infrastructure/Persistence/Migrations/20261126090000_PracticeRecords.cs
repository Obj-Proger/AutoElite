using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoElite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the practice records of the students: one row per student and question of the bank, with the answers, the
    /// wrong ones and whether the last answer was right.
    /// </summary>
    public partial class PracticeRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "practice_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    mistake_count = table.Column<int>(type: "integer", nullable: false),
                    is_last_answer_correct = table.Column<bool>(type: "boolean", nullable: false),
                    last_answered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_practice_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_practice_records_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_practice_records_users_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_practice_records_question_id",
                table: "practice_records",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_practice_records_student_id_question_id",
                table: "practice_records",
                columns: new[] { "student_id", "question_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "practice_records");
        }
    }
}
