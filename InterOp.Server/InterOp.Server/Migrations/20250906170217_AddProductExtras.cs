using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterOp.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProductExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryId2",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pic",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reviews",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sales",
                table: "Products",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId2",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Pic",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Reviews",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sales",
                table: "Products");
        }
    }
}
