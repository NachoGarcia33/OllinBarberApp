using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OllinBarberApp.Data;

#nullable disable

namespace OllinBarberApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260821000100_TiendaPedidosSeguridad")]
    public partial class TiendaPedidosSeguridad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenConfirmacion",
                table: "Citas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "FechaHora",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "Productos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Productos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Productos_Descuento",
                table: "Productos",
                sql: "\"DescuentoPorcentaje\" >= 0 AND \"DescuentoPorcentaje\" <= 100");

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: true),
                    ClienteNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MetodoEntrega = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notas = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    DescuentoTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    DescuentoUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalles", x => x.Id);
                    table.CheckConstraint("CK_PedidoDetalles_Cantidad", "\"Cantidad\" > 0");
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_Citas_FechaHora", table: "Citas", column: "FechaHora");
            migrationBuilder.CreateIndex(name: "IX_Citas_TokenConfirmacion", table: "Citas", column: "TokenConfirmacion", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Productos_Activo_Categoria_Marca", table: "Productos", columns: new[] { "Activo", "Categoria", "Marca" });
            migrationBuilder.CreateIndex(name: "IX_PedidoDetalles_PedidoId", table: "PedidoDetalles", column: "PedidoId");
            migrationBuilder.CreateIndex(name: "IX_PedidoDetalles_ProductoId", table: "PedidoDetalles", column: "ProductoId");
            migrationBuilder.CreateIndex(name: "IX_Pedidos_Codigo", table: "Pedidos", column: "Codigo", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Pedidos_Estado", table: "Pedidos", column: "Estado");
            migrationBuilder.CreateIndex(name: "IX_Pedidos_Fecha", table: "Pedidos", column: "Fecha");
            migrationBuilder.CreateIndex(name: "IX_Pedidos_UsuarioId", table: "Pedidos", column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PedidoDetalles");
            migrationBuilder.DropTable(name: "Pedidos");

            migrationBuilder.DropIndex(name: "IX_Citas_FechaHora", table: "Citas");
            migrationBuilder.DropIndex(name: "IX_Citas_TokenConfirmacion", table: "Citas");
            migrationBuilder.DropIndex(name: "IX_Productos_Activo_Categoria_Marca", table: "Productos");

            migrationBuilder.DropCheckConstraint(name: "CK_Productos_Descuento", table: "Productos");

            migrationBuilder.DropColumn(name: "TokenConfirmacion", table: "Citas");
            migrationBuilder.DropColumn(name: "DescuentoPorcentaje", table: "Productos");
            migrationBuilder.DropColumn(name: "Marca", table: "Productos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHora",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
