using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleRights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Right",
                schema: "Auth",
                columns: table => new
                {
                    RightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK-Auth_Right_RightId", x => x.RightId);
                });

            migrationBuilder.CreateTable(
                name: "RoleRight",
                schema: "Auth",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK-Auth_RoleRight_RoleId_RightId", x => new { x.RoleId, x.RightId });
                    table.ForeignKey(
                        name: "FK-Auth_RoleRight_Right_Right",
                        column: x => x.RightId,
                        principalSchema: "Auth",
                        principalTable: "Right",
                        principalColumn: "RightId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK-Auth_RoleRight_Role_ApplicationRole",
                        column: x => x.RoleId,
                        principalSchema: "Auth",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX-Auth_Right_Code",
                schema: "Auth",
                table: "Right",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleRight_RightId",
                schema: "Auth",
                table: "RoleRight",
                column: "RightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleRight",
                schema: "Auth");

            migrationBuilder.DropTable(
                name: "Right",
                schema: "Auth");
        }
    }
}
