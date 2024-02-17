using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersService.Migrations
{
    /// <inheritdoc />
    public partial class NewVariantOfAuthTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    postCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    firstName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    middleName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    lastName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AuthInfo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    login = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    passwordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    passwordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    jwtId = table.Column<Guid>(type: "uuid", nullable: false),
                    refreshTokenHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    refreshTokenSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    refreshTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthInfo", x => x.id);
                    table.ForeignKey(
                        name: "FK_AuthInfo_Users_id",
                        column: x => x.id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthInfo");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
