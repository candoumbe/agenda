using Microsoft.EntityFrameworkCore.Migrations;
using Paramore.Brighter.Outbox.PostgreSql;

#nullable disable

namespace Agenda.DataStores.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTable : Migration
    {
        private const string OutboxTableName = "outbox";
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string ddl = PostgreSqlOutboxBuilder.GetDDL(OutboxTableName);
            migrationBuilder.Sql(ddl);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(OutboxTableName);
        }
    }
}