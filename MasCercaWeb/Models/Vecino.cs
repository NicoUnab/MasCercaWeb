using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MasCercaWeb.Models
{
    public class Vecino
    {
        [Key] // Clave primaria
        [ForeignKey("Usuario")] // También es una clave foránea hacia Usuario
        public int id { get; set; }
        public string direccion { get; set; }
        // Relación con Usuario
        public Usuario Usuario { get; set; }
        public ICollection<Reporte> Reportes { get; set; }
    }
}
