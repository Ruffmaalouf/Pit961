using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    billing_email = table.Column<string>(type: "text", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "text", nullable: true),
                    subscription_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    plan = table.Column<string>(type: "text", nullable: false),
                    trial_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_subscription_status", "subscription_status IN ('trial','active','past_due','suspended','cancelled','expired')");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: false),
                    whatsapp = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_fleet = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estimate_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    part_number = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    approval_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimate_items", x => x.id);
                    table.CheckConstraint("ck_estimate_items_approval_status", "approval_status IN ('pending','approved','rejected')");
                    table.CheckConstraint("ck_estimate_items_type", "type IN ('part','labor','service','misc')");
                });

            migrationBuilder.CreateTable(
                name: "estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    parent_estimate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    approval_method = table.Column<string>(type: "text", nullable: true),
                    approved_by_name = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimates", x => x.id);
                    table.CheckConstraint("ck_estimates_status", "status IN ('draft','sent','approved','partially_approved','rejected','superseded')");
                    table.ForeignKey(
                        name: "fk_estimates_estimates_parent_estimate_id",
                        column: x => x.parent_estimate_id,
                        principalTable: "estimates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "garage_sequences",
                columns: table => new
                {
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_job_number = table.Column<long>(type: "bigint", nullable: false),
                    next_invoice_number = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garage_sequences", x => x.garage_id);
                });

            migrationBuilder.CreateTable(
                name: "garage_settings",
                columns: table => new
                {
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_label = table.Column<string>(type: "text", nullable: false),
                    default_labor_rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    invoice_prefix = table.Column<string>(type: "text", nullable: false),
                    working_hours_open = table.Column<string>(type: "text", nullable: false),
                    working_hours_close = table.Column<string>(type: "text", nullable: false),
                    diagnosis_fee_policy = table.Column<string>(type: "text", nullable: false),
                    diagnosis_fee_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    warranty_period_days = table.Column<int>(type: "integer", nullable: false),
                    warranty_mileage_km = table.Column<int>(type: "integer", nullable: false),
                    allow_delivery_with_balance = table.Column<bool>(type: "boolean", nullable: false),
                    display_currency = table.Column<string>(type: "text", nullable: true),
                    discount_limit_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    estimate_approval_threshold = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    extra_settings = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garage_settings", x => x.garage_id);
                });

            migrationBuilder.CreateTable(
                name: "garages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garages", x => x.id);
                    table.ForeignKey(
                        name: "fk_garages_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    last_login = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN ('owner','manager','advisor','mechanic','accountant')");
                    table.ForeignKey(
                        name: "fk_users_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate_number = table.Column<string>(type: "text", nullable: false),
                    plate_country = table.Column<string>(type: "text", nullable: false),
                    make = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    vin = table.Column<string>(type: "text", nullable: true),
                    engine = table.Column<string>(type: "text", nullable: true),
                    engine_code = table.Column<string>(type: "text", nullable: true),
                    transmission = table.Column<string>(type: "text", nullable: true),
                    drivetrain = table.Column<string>(type: "text", nullable: true),
                    fuel_type = table.Column<string>(type: "text", nullable: true),
                    current_mileage = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vehicles_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_refresh_tokens_replaced_by_token_id",
                        column: x => x.replaced_by_token_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_number = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_mechanic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    secondary_mechanic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mileage_at_intake = table.Column<int>(type: "integer", nullable: true),
                    customer_complaint = table.Column<string>(type: "text", nullable: true),
                    advisor_notes = table.Column<string>(type: "text", nullable: true),
                    promised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    customer_waiting = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    overnight = table.Column<bool>(type: "boolean", nullable: false),
                    overnight_note = table.Column<string>(type: "text", nullable: true),
                    is_warranty_return = table.Column<bool>(type: "boolean", nullable: false),
                    parent_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    deletion_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.CheckConstraint("ck_jobs_status", "status IN ('checked_in','diagnosing','waiting_approval','waiting_parts','ready_to_repair','repairing','qc','ready','delivered','cancelled')");
                    table.ForeignKey(
                        name: "fk_jobs_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_jobs_parent_job_id",
                        column: x => x.parent_job_id,
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_users_primary_mechanic_id",
                        column: x => x.primary_mechanic_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_users_secondary_mechanic_id",
                        column: x => x.secondary_mechanic_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_paid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    display_rate_snapshot = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.CheckConstraint("ck_invoices_status", "status IN ('unpaid','partial','paid','voided','written_off')");
                    table.ForeignKey(
                        name: "fk_invoices_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_invoices_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_invoices_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_invoices_users_voided_by",
                        column: x => x.voided_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "text", nullable: false),
                    actor_role = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    detail = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_history_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_history_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_history_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    part_number = table.Column<string>(type: "text", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_name_snapshot = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    supplied_by = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ordered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    arrived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    installed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    return_reason = table.Column<string>(type: "text", nullable: true),
                    issue_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_parts", x => x.id);
                    table.CheckConstraint("ck_job_parts_status", "status IN ('needed','searching','ordered','arrived','installed','returned','issue_wrong_part','issue_damaged')");
                    table.ForeignKey(
                        name: "fk_job_parts_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_parts_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "repair_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assigned_mechanic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    outsourced = table.Column<bool>(type: "boolean", nullable: false),
                    outsource_supplier = table.Column<string>(type: "text", nullable: true),
                    outsource_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    outsource_billed = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    outsource_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outsource_returned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repair_tasks", x => x.id);
                    table.CheckConstraint("ck_repair_tasks_status", "status IN ('pending','in_progress','paused','completed','cancelled')");
                    table.ForeignKey(
                        name: "fk_repair_tasks_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_repair_tasks_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_repair_tasks_users_assigned_mechanic_id",
                        column: x => x.assigned_mechanic_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_method", "method IN ('cash','card','bank_transfer','cheque','other')");
                    table.ForeignKey(
                        name: "fk_payments_garages_garage_id",
                        column: x => x.garage_id,
                        principalTable: "garages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_payments_users_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "customers_garage_idx",
                table: "customers",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "customers_phone_idx",
                table: "customers",
                columns: new[] { "garage_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "estimate_items_estimate_idx",
                table: "estimate_items",
                column: "estimate_id");

            migrationBuilder.CreateIndex(
                name: "estimate_items_garage_idx",
                table: "estimate_items",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "estimates_garage_idx",
                table: "estimates",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "estimates_job_idx",
                table: "estimates",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "estimates_parent_idx",
                table: "estimates",
                column: "parent_estimate_id",
                filter: "parent_estimate_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_estimates_created_by",
                table: "estimates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "garages_account_active_idx",
                table: "garages",
                column: "account_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_garages_deleted_by",
                table: "garages",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "invoices_job_idx",
                table: "invoices",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "invoices_number_idx",
                table: "invoices",
                columns: new[] { "garage_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_created_by",
                table: "invoices",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_voided_by",
                table: "invoices",
                column: "voided_by");

            migrationBuilder.CreateIndex(
                name: "ix_job_history_actor_id",
                table: "job_history",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "job_history_garage_idx",
                table: "job_history",
                columns: new[] { "garage_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "job_history_job_idx",
                table: "job_history",
                columns: new[] { "job_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "job_parts_garage_idx",
                table: "job_parts",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "job_parts_job_idx",
                table: "job_parts",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_cancelled_by",
                table: "jobs",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_created_by",
                table: "jobs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_deleted_by",
                table: "jobs",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "jobs_customer_idx",
                table: "jobs",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "jobs_garage_status_idx",
                table: "jobs",
                columns: new[] { "garage_id", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "jobs_mechanic_idx",
                table: "jobs",
                column: "primary_mechanic_id");

            migrationBuilder.CreateIndex(
                name: "jobs_number_idx",
                table: "jobs",
                columns: new[] { "garage_id", "job_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "jobs_parent_job_idx",
                table: "jobs",
                column: "parent_job_id",
                filter: "parent_job_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "jobs_secondary_mechanic_idx",
                table: "jobs",
                column: "secondary_mechanic_id",
                filter: "secondary_mechanic_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "jobs_vehicle_idx",
                table: "jobs",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_recorded_by",
                table: "payments",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "payments_idempotency_idx",
                table: "payments",
                columns: new[] { "garage_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "payments_invoice_idx",
                table: "payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_replaced_by_token_id",
                table: "refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "refresh_tokens_token_hash_idx",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "refresh_tokens_user_idx",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "repair_tasks_garage_idx",
                table: "repair_tasks",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "repair_tasks_job_idx",
                table: "repair_tasks",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "repair_tasks_mechanic_idx",
                table: "repair_tasks",
                column: "assigned_mechanic_id",
                filter: "assigned_mechanic_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "users_email_idx",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_garage_idx",
                table: "users",
                column: "garage_id");

            migrationBuilder.CreateIndex(
                name: "vehicles_customer_idx",
                table: "vehicles",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "vehicles_plate_idx",
                table: "vehicles",
                columns: new[] { "garage_id", "plate_number", "plate_country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "vehicles_vin_idx",
                table: "vehicles",
                columns: new[] { "garage_id", "vin" });

            migrationBuilder.AddForeignKey(
                name: "fk_customers_garages_garage_id",
                table: "customers",
                column: "garage_id",
                principalTable: "garages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_estimate_items_estimates_estimate_id",
                table: "estimate_items",
                column: "estimate_id",
                principalTable: "estimates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_estimate_items_garages_garage_id",
                table: "estimate_items",
                column: "garage_id",
                principalTable: "garages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_estimates_garages_garage_id",
                table: "estimates",
                column: "garage_id",
                principalTable: "garages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_estimates_jobs_job_id",
                table: "estimates",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_estimates_users_created_by",
                table: "estimates",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_garage_sequences_garages_garage_id",
                table: "garage_sequences",
                column: "garage_id",
                principalTable: "garages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_garage_settings_garages_garage_id",
                table: "garage_settings",
                column: "garage_id",
                principalTable: "garages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_garages_users_deleted_by",
                table: "garages",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_garages_garage_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "estimate_items");

            migrationBuilder.DropTable(
                name: "garage_sequences");

            migrationBuilder.DropTable(
                name: "garage_settings");

            migrationBuilder.DropTable(
                name: "job_history");

            migrationBuilder.DropTable(
                name: "job_parts");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "repair_tasks");

            migrationBuilder.DropTable(
                name: "estimates");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "garages");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
