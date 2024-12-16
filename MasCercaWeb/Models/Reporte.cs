namespace MasCercaWeb.Models
{
    public class Reporte
    {
        public int id { get; set; }
        public string descripcion { get; set; }
        public string ubicacion { get; set; }
        public DateTime fechaCreacion { get; set; }
        public int imagen { get; set; }
        public int idVecino { get; set; }
        // Propiedad para el último usuario
        public string ultimaActualizacion { get; set; }
        // Relaciones
        public Vecino Vecino { get; set; }
        public EstadoReporte Estado { get; set; }
        public TipoReporte Tipo { get; set; }
        public ICollection<HistorialReporte> HistorialReportes { get; set; }
    }
}
