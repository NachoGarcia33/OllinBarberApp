using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OllinBarberApp.Data;

#nullable disable

namespace OllinBarberApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260928010000_AgregarNotificacionesYPush")]
    public partial class AgregarNotificacionesYPush : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BarberoId = table.Column<int>(type: "integer", nullable: false),
                    CitaId = table.Column<int>(type: "integer", nullable: true),
                    Mensaje = table.Column<string>(type: "text", maxLength: 300, nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuscripcionesPush",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BarberoId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuscripcionesPush", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuscripcionesPush_Barberos_BarberoId",
                        column: x => x.BarberoId,
                        principalTable: "Barberos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_Notificaciones_BarberoId_Leida", table: "Notificaciones", columns: new[] { "BarberoId", "Leida" });
            migrationBuilder.CreateIndex(name: "IX_Notificaciones_CitaId", table: "Notificaciones", column: "CitaId");
            migrationBuilder.CreateIndex(name: "IX_SuscripcionesPush_BarberoId", table: "SuscripcionesPush", column: "BarberoId");
            migrationBuilder.CreateIndex(name: "IX_SuscripcionesPush_Endpoint", table: "SuscripcionesPush", column: "Endpoint", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Notificaciones");
            migrationBuilder.DropTable(name: "SuscripcionesPush");
        }
    }
}
