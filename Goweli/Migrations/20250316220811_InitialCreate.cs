using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Goweli.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Synopsis = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoverUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorName",
                table: "Books",
                column: "AuthorName");

            migrationBuilder.CreateIndex(
                name: "IX_Books_BookTitle",
                table: "Books",
                column: "BookTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
