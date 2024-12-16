namespace MasCercaWeb.Models
{
    public class HistorialReporte
    {
        public int id { get; set; }
        public DateTime fecha { get; set; }
        public string observacion { get; set; }
        public int idReporte { get; set; }
        public int idFuncionario { get; set; }
        // Relaciones
        public Reporte Reporte { get; set; }
        public FuncionarioMunicipal FuncionarioMunicipal { get; set; }
    }
}
