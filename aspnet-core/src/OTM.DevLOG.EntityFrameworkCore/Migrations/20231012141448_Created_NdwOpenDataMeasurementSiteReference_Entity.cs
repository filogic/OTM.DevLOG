using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OTM.DevLOG.Migrations
{
    /// <inheritdoc />
    public partial class CreatedNdwOpenDataMeasurementSiteReferenceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NdwOpenDataMeasurementSiteReference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasurementSiteId = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    MeasurementSiteReference = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NdwOpenDataMeasurementSiteReference", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NdwOpenDataMeasurementSiteReference");
        }
    }
}
