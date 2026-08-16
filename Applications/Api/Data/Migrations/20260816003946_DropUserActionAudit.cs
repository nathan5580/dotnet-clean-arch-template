using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUserActionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActionAudit",
                schema: "Auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActionAudit",
                schema: "Auth",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK-Auth_UserActionAudit_AuditId", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK-Auth_UserActionAudit_UserId_UserId",
                        column: x => x.UserId,
                        principalSchema: "Auth",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX-Auth_UserActionAudit_Timestamp",
                schema: "Auth",
                table: "UserActionAudit",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX-Auth_UserActionAudit_UserId",
                schema: "Auth",
                table: "UserActionAudit",
                column: "UserId");
        }
    }
}
