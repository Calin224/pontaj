using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proiecte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nume = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proiecte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZileDeLucru",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZileDeLucru", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pontaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZiDeLucruId = table.Column<int>(type: "int", nullable: false),
                    OraInceput = table.Column<TimeSpan>(type: "time", nullable: false),
                    OraSfarsit = table.Column<TimeSpan>(type: "time", nullable: false),
                    TipMunca = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProiectId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pontaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pontaje_Proiecte_ProiectId",
                        column: x => x.ProiectId,
                        principalTable: "Proiecte",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pontaje_ZileDeLucru_ZiDeLucruId",
                        column: x => x.ZiDeLucruId,
                        principalTable: "ZileDeLucru",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pontaje_ProiectId",
                table: "Pontaje",
                column: "ProiectId");

            migrationBuilder.CreateIndex(
                name: "IX_Pontaje_ZiDeLucruId",
                table: "Pontaje",
                column: "ZiDeLucruId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pontaje");

            migrationBuilder.DropTable(
                name: "Proiecte");

            migrationBuilder.DropTable(
                name: "ZileDeLucru");
        }
    }
}
