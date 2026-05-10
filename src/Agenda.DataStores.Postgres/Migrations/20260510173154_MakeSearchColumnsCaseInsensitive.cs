using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agenda.DataStores.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class MakeSearchColumnsCaseInsensitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Appointments",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Appointments",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Appointments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Appointments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
