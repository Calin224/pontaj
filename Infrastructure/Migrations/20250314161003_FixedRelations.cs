using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "ZileDeLucru",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Pontaje",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Pontaje",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppUserProiect",
                columns: table => new
                {
                    ProiecteId = table.Column<int>(type: "int", nullable: false),
                    UseriId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserProiect", x => new { x.ProiecteId, x.UseriId });
                    table.ForeignKey(
                        name: "FK_AppUserProiect_AspNetUsers_UseriId",
                        column: x => x.UseriId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserProiect_Proiecte_ProiecteId",
                        column: x => x.ProiecteId,
                        principalTable: "Proiecte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZileDeLucru_AppUserId",
                table: "ZileDeLucru",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Pontaje_AppUserId",
                table: "Pontaje",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserProiect_UseriId",
                table: "AppUserProiect",
                column: "UseriId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pontaje_AspNetUsers_AppUserId",
                table: "Pontaje",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ZileDeLucru_AspNetUsers_AppUserId",
                table: "ZileDeLucru",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pontaje_AspNetUsers_AppUserId",
                table: "Pontaje");

            migrationBuilder.DropForeignKey(
                name: "FK_ZileDeLucru_AspNetUsers_AppUserId",
                table: "ZileDeLucru");

            migrationBuilder.DropTable(
                name: "AppUserProiect");

            migrationBuilder.DropIndex(
                name: "IX_ZileDeLucru_AppUserId",
                table: "ZileDeLucru");

            migrationBuilder.DropIndex(
                name: "IX_Pontaje_AppUserId",
                table: "Pontaje");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "ZileDeLucru");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Pontaje");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Pontaje");
        }
    }
}
