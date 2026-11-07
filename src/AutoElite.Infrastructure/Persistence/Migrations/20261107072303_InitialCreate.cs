using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoElite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        // Written by hand: the scripts of the objects EF Core does not model, as they were when this migration was made.
        private static readonly string[] Scripts =
        [
            "01-theory-lesson-overlap",
            "02-driving-overlap",
            "03-notification-subject",
            "04-staff-task-subject",
            "05-document-owner"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_number_counters",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_number_counters", x => x.year);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    theory_hours = table.Column<int>(type: "integer", nullable: false),
                    practice_hours = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "queued_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queued_emails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quality_window_size = table.Column<int>(type: "integer", nullable: false),
                    low_rating_threshold = table.Column<int>(type: "integer", nullable: false),
                    student_cancellation_notice = table.Column<TimeSpan>(type: "interval", nullable: false),
                    lesson_reminder_lead_times = table.Column<TimeSpan[]>(type: "interval[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    singleton = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_settings", x => x.id);
                    table.CheckConstraint("ck_school_settings_singleton", "singleton");
                });

            migrationBuilder.CreateTable(
                name: "test_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    blocks = table.Column<string>(type: "jsonb", nullable: false),
                    rules_extra_questions_per_mistake = table.Column<int>(type: "integer", nullable: false),
                    rules_extra_time_per_mistake = table.Column<TimeSpan>(type: "interval", nullable: false),
                    rules_max_mistakes = table.Column<int>(type: "integer", nullable: false),
                    rules_max_mistakes_per_block = table.Column<int>(type: "integer", nullable: true),
                    rules_question_count = table.Column<int>(type: "integer", nullable: false),
                    rules_time_limit = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "training_grounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_training_grounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_grounds_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    transmission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "course_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_topics", x => x.id);
                    table.ForeignKey(
                        name: "fk_course_topics_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answer_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answer_options", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false),
                    last_read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    attachment_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    direct_key = table.Column<string>(type: "character varying(65)", maxLength: 65, nullable: true),
                    study_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    transmission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_practice_hours = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    attended_theory_lessons = table.Column<int>(type: "integer", nullable: false),
                    recent_grades = table.Column<decimal[]>(type: "numeric(3,2)[]", nullable: false),
                    quality_score = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    closed_on = table.Column<DateOnly>(type: "date", nullable: true),
                    termination_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    paid_amount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    paid_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_course_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    image_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, collation: "case_insensitive"),
                    categories = table.Column<int[]>(type: "integer[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_questions_course_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "course_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_questions_documents_image_document_id",
                        column: x => x.image_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    photo_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    full_name_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_documents_photo_document_id",
                        column: x => x.photo_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "driving_routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_ground_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driving_routes", x => x.id);
                    table.ForeignKey(
                        name: "fk_driving_routes_training_ground_training_ground_id",
                        column: x => x.training_ground_id,
                        principalTable: "training_grounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_routes_user_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    desired_category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    lost_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.ForeignKey(
                        name: "fk_leads_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_user_manager_id",
                        column: x => x.manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_user_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    registered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_user_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recurrence = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    overdue_notified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_tasks_user_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "study_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    theory_instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_groups_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_study_groups_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_study_groups_user_theory_instructor_id",
                        column: x => x.theory_instructor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_accounts_user_id",
                        column: x => x.id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "driving_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    transmission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    training_ground_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driving_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_driving_slots_driving_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "driving_routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_slots_training_ground_training_ground_id",
                        column: x => x.training_ground_id,
                        principalTable: "training_grounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_slots_user_instructor_id",
                        column: x => x.instructor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_slots_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lead_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    written_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_lead_notes_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lead_notes_user_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lead_status_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    to_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_status_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_lead_status_changes_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lead_status_changes_user_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    test_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exams", x => x.id);
                    table.ForeignKey(
                        name: "fk_exams_study_group_group_id",
                        column: x => x.group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exams_test_template_test_template_id",
                        column: x => x.test_template_id,
                        principalTable: "test_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    study_group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_enrollments_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_enrollments_study_group_study_group_id",
                        column: x => x.study_group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_enrollments_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "theory_lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_theory_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_theory_lessons_course_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "course_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_theory_lessons_study_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "study_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_theory_lessons_user_instructor_id",
                        column: x => x.instructor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_account_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_account_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_account_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_account_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_account_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_account_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_account_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "driving_bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    actual_hours = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    instructor_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    student_rating = table.Column<int>(type: "integer", nullable: true),
                    student_comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    rated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    driving_slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_grades = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driving_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_driving_bookings_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_bookings_driving_slot_driving_slot_id",
                        column: x => x.driving_slot_id,
                        principalTable: "driving_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_driving_bookings_user_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_driving_bookings_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    pending_add_on_blocks = table.Column<int[]>(type: "integer[]", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deadline_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    blocks = table.Column<string>(type: "jsonb", nullable: false),
                    rules_extra_questions_per_mistake = table.Column<int>(type: "integer", nullable: false),
                    rules_extra_time_per_mistake = table.Column<TimeSpan>(type: "interval", nullable: false),
                    rules_max_mistakes = table.Column<int>(type: "integer", nullable: false),
                    rules_max_mistakes_per_block = table.Column<int>(type: "integer", nullable: true),
                    rules_question_count = table.Column<int>(type: "integer", nullable: false),
                    rules_time_limit = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_sessions_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_sessions_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_sessions_test_template_template_id",
                        column: x => x.template_id,
                        principalTable: "test_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_test_sessions_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lesson_attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_present = table.Column<bool>(type: "boolean", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    marked_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    theory_lesson_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_attendance", x => x.id);
                    table.ForeignKey(
                        name: "fk_lesson_attendance_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lesson_attendance_theory_lesson_theory_lesson_id",
                        column: x => x.theory_lesson_id,
                        principalTable: "theory_lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lesson_attendance_user_marked_by_user_id",
                        column: x => x.marked_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lesson_attendance_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    test_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    corrected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corrected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exam_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_results_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_results_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exam_results_test_session_test_session_id",
                        column: x => x.test_session_id,
                        principalTable: "test_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_results_user_corrected_by_user_id",
                        column: x => x.corrected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_results_user_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_results_user_student_id",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    block_number = table.Column<int>(type: "integer", nullable: false),
                    is_add_on = table.Column<bool>(type: "boolean", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    image_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    correct_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    test_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_session_questions_documents_image_document_id",
                        column: x => x.image_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_session_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_session_questions_test_session_test_session_id",
                        column: x => x.test_session_id,
                        principalTable: "test_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_answer_options_question_id",
                table: "answer_options",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_branches_name",
                table: "branches",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_members_user_id",
                table: "chat_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_chat_members_chat_user_current",
                table: "chat_members",
                columns: new[] { "chat_id", "user_id" },
                unique: true,
                filter: "left_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_attachment_ids",
                table: "chat_messages",
                column: "attachment_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_chat_id_sent_at_utc_id",
                table: "chat_messages",
                columns: new[] { "chat_id", "sent_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_sender_id",
                table: "chat_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_chats_direct_key",
                table: "chats",
                column: "direct_key",
                unique: true,
                filter: "direct_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chats_study_group_id",
                table: "chats",
                column: "study_group_id",
                unique: true,
                filter: "study_group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_branch_id",
                table: "contracts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_course_id",
                table: "contracts",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_number",
                table: "contracts",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_start_date",
                table: "contracts",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_student_id",
                table: "contracts",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_topics_course_id",
                table: "course_topics",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_storage_key",
                table: "documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by_user_id",
                table: "documents",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_bookings_cancelled_by_user_id",
                table: "driving_bookings",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_bookings_contract_id",
                table: "driving_bookings",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_bookings_driving_slot_id",
                table: "driving_bookings",
                column: "driving_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_bookings_student_id",
                table: "driving_bookings",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_routes_author_id",
                table: "driving_routes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_routes_training_ground_id",
                table: "driving_routes",
                column: "training_ground_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_slots_instructor_id",
                table: "driving_slots",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_slots_route_id",
                table: "driving_slots",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_slots_status",
                table: "driving_slots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_driving_slots_training_ground_id",
                table: "driving_slots",
                column: "training_ground_id");

            migrationBuilder.CreateIndex(
                name: "ix_driving_slots_vehicle_id",
                table: "driving_slots",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_contract_id",
                table: "exam_results",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_corrected_by_user_id",
                table: "exam_results",
                column: "corrected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_exam_id_contract_id",
                table: "exam_results",
                columns: new[] { "exam_id", "contract_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_recorded_by_user_id",
                table: "exam_results",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_student_id",
                table: "exam_results",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_results_test_session_id",
                table: "exam_results",
                column: "test_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_exams_group_id",
                table: "exams",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_exams_scheduled_at_utc",
                table: "exams",
                column: "scheduled_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_exams_test_template_id",
                table: "exams",
                column: "test_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_enrollments_contract_id",
                table: "group_enrollments",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_enrollments_student_id",
                table: "group_enrollments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_enrollments_study_group_id_contract_id",
                table: "group_enrollments",
                columns: new[] { "study_group_id", "contract_id" },
                unique: true,
                filter: "withdrawn_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lead_notes_author_id",
                table: "lead_notes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_notes_lead_id",
                table: "lead_notes",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_status_changes_changed_by_user_id",
                table: "lead_status_changes",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_status_changes_lead_id",
                table: "lead_status_changes",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_branch_id",
                table: "leads",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_created_at_utc",
                table: "leads",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_leads_manager_id",
                table: "leads",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_status_created_at_utc",
                table: "leads",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_lesson_attendance_contract_id",
                table: "lesson_attendance",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_attendance_marked_by_user_id",
                table: "lesson_attendance",
                column: "marked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_attendance_student_id",
                table: "lesson_attendance",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_attendance_theory_lesson_id_contract_id",
                table: "lesson_attendance",
                columns: new[] { "theory_lesson_id", "contract_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_id_created_at_utc",
                table: "notifications",
                columns: new[] { "recipient_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_id_unread",
                table: "notifications",
                column: "recipient_id",
                filter: "read_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_on_utc",
                table: "outbox_messages",
                column: "next_attempt_on_utc",
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payments_contract_id",
                table: "payments",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_external_reference",
                table: "payments",
                column: "external_reference");

            migrationBuilder.CreateIndex(
                name: "ix_payments_registered_at_utc",
                table: "payments",
                column: "registered_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_payments_registered_by_user_id",
                table: "payments",
                column: "registered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_student_id",
                table: "payments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_external_code",
                table: "questions",
                column: "external_code",
                unique: true,
                filter: "external_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_questions_image_document_id",
                table: "questions",
                column: "image_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_topic_id",
                table: "questions",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "ix_queued_emails_next_attempt_at_utc",
                table: "queued_emails",
                column: "next_attempt_at_utc",
                filter: "sent_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_school_settings_singleton",
                table: "school_settings",
                column: "singleton",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_session_questions_image_document_id",
                table: "session_questions",
                column: "image_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_session_questions_question_id",
                table: "session_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_session_questions_test_session_id",
                table: "session_questions",
                column: "test_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_tasks_assignee_id_due_at_utc",
                table: "staff_tasks",
                columns: new[] { "assignee_id", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_tasks_due_at_utc_open",
                table: "staff_tasks",
                column: "due_at_utc",
                filter: "status <> 'Done'");

            migrationBuilder.CreateIndex(
                name: "ix_study_groups_branch_id",
                table: "study_groups",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_groups_course_id",
                table: "study_groups",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_groups_start_date",
                table: "study_groups",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "ix_study_groups_theory_instructor_id",
                table: "study_groups",
                column: "theory_instructor_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_exam_id_contract_id",
                table: "test_sessions",
                columns: new[] { "exam_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_student_id",
                table: "test_sessions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_sessions_template_id",
                table: "test_sessions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ux_test_sessions_contract_in_progress",
                table: "test_sessions",
                column: "contract_id",
                unique: true,
                filter: "status = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "ix_theory_lessons_group_id",
                table: "theory_lessons",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_theory_lessons_instructor_id",
                table: "theory_lessons",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "ix_theory_lessons_status",
                table: "theory_lessons",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_theory_lessons_topic_id",
                table: "theory_lessons",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "ix_training_grounds_branch_id",
                table: "training_grounds",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_account_claims_user_id",
                table: "user_account_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_account_logins_user_id",
                table: "user_account_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "user_accounts",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "user_accounts",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_user_id_role",
                table: "user_role_assignments",
                columns: new[] { "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_branch_id",
                table: "users",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_photo_document_id",
                table: "users",
                column: "photo_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_branch_id",
                table: "vehicles",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_plate",
                table: "vehicles",
                column: "plate",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_answer_options_question_question_id",
                table: "answer_options",
                column: "question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_members_chats_chat_id",
                table: "chat_members",
                column: "chat_id",
                principalTable: "chats",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_members_user_user_id",
                table: "chat_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_chats_chat_id",
                table: "chat_messages",
                column: "chat_id",
                principalTable: "chats",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_users_sender_id",
                table: "chat_messages",
                column: "sender_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chats_study_group_study_group_id",
                table: "chats",
                column: "study_group_id",
                principalTable: "study_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contracts_user_student_id",
                table: "contracts",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_user_uploaded_by_user_id",
                table: "documents",
                column: "uploaded_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Written by hand: the objects EF Core does not model (see DatabaseObjects).
            DatabaseObjects.Create(migrationBuilder, Scripts);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Written by hand: the objects EF Core does not model go first, since they depend on the tables.
            DatabaseObjects.Drop(migrationBuilder, Scripts);

            migrationBuilder.DropForeignKey(
                name: "fk_documents_user_uploaded_by_user_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "answer_options");

            migrationBuilder.DropTable(
                name: "chat_members");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "contract_number_counters");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "driving_bookings");

            migrationBuilder.DropTable(
                name: "exam_results");

            migrationBuilder.DropTable(
                name: "group_enrollments");

            migrationBuilder.DropTable(
                name: "lead_notes");

            migrationBuilder.DropTable(
                name: "lead_status_changes");

            migrationBuilder.DropTable(
                name: "lesson_attendance");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "queued_emails");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "school_settings");

            migrationBuilder.DropTable(
                name: "session_questions");

            migrationBuilder.DropTable(
                name: "staff_tasks");

            migrationBuilder.DropTable(
                name: "user_account_claims");

            migrationBuilder.DropTable(
                name: "user_account_logins");

            migrationBuilder.DropTable(
                name: "user_account_tokens");

            migrationBuilder.DropTable(
                name: "user_role_assignments");

            migrationBuilder.DropTable(
                name: "chats");

            migrationBuilder.DropTable(
                name: "driving_slots");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "theory_lessons");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "test_sessions");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "driving_routes");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "course_topics");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "training_grounds");

            migrationBuilder.DropTable(
                name: "study_groups");

            migrationBuilder.DropTable(
                name: "test_templates");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "documents");

            // Written by hand: Up creates the collation, and EF Core does not drop it, so that Up can run again.
            migrationBuilder.Sql("DROP COLLATION IF EXISTS case_insensitive;");
        }
    }
}
