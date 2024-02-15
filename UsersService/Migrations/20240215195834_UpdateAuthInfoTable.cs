using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuthInfoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "post_code",
                table: "Users",
                newName: "postCode");

            migrationBuilder.RenameColumn(
                name: "middle_name",
                table: "Users",
                newName: "middleName");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "Users",
                newName: "lastName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "Users",
                newName: "firstName");

            migrationBuilder.RenameColumn(
                name: "password_salt",
                table: "AuthInfo",
                newName: "refreshTokenSalt");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AuthInfo",
                newName: "refreshTokenHash");

            migrationBuilder.RenameColumn(
                name: "jwt_id",
                table: "AuthInfo",
                newName: "jwtId");

            migrationBuilder.AddColumn<byte[]>(
                name: "passwordHash",
                table: "AuthInfo",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "passwordSalt",
                table: "AuthInfo",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "refreshTokenExpiry",
                table: "AuthInfo",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "passwordHash",
                table: "AuthInfo");

            migrationBuilder.DropColumn(
                name: "passwordSalt",
                table: "AuthInfo");

            migrationBuilder.DropColumn(
                name: "refreshTokenExpiry",
                table: "AuthInfo");

            migrationBuilder.RenameColumn(
                name: "postCode",
                table: "Users",
                newName: "post_code");

            migrationBuilder.RenameColumn(
                name: "middleName",
                table: "Users",
                newName: "middle_name");

            migrationBuilder.RenameColumn(
                name: "lastName",
                table: "Users",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "firstName",
                table: "Users",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "refreshTokenSalt",
                table: "AuthInfo",
                newName: "password_salt");

            migrationBuilder.RenameColumn(
                name: "refreshTokenHash",
                table: "AuthInfo",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "jwtId",
                table: "AuthInfo",
                newName: "jwt_id");
        }
    }
}
