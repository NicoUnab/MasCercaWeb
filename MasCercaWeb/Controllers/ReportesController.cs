using MasCercaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace MasCercaWeb.Controllers
{
    public class ReportesController : Controller
    {
        private readonly IConfiguration _configuration;

        public ReportesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var userData = HttpContext.Session.GetString("UserData");
            if (userData == null)
                return RedirectToAction("Index", "Login");

            var user = JsonConvert.DeserializeObject<dynamic>(userData); // Deserializa el userData
            int funcionarioId = user.Id; // Asumiendo que el ID está dentro de userData
            ViewBag.UserData = JsonConvert.DeserializeObject(userData);
            List<Reporte> reportes = new List<Reporte>();
            try
            {
                var token = HttpContext.Session.GetString("Token");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Index", "Login");
                }

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["ApiUrls:GestionReportes"]);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Traer todos los reportes asignados al Funcionario
                    var response = await client.GetAsync($"api/Reporte/lista-reportes");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        reportes = JsonConvert.DeserializeObject<List<Reporte>>(await response.Content.ReadAsStringAsync());
                        reportes = reportes.Where(r => r.Estado.nombre != "Finalizado").ToList();
                        ViewBag.Reportes = reportes;
                    }
                    else
                    {
                        ViewBag.Reportes = reportes;
                        ViewBag.Error = "Error al cargar los reportes.";                        
                    }
                }
            }
            catch
            {
                ViewBag.Reportes = reportes;
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AtenderReporte([FromBody] AtenderReporteDTO dto)
        {            
            var userData = HttpContext.Session.GetString("UserData");
            if (userData == null)
                return RedirectToAction("Index", "Login");
            var user = JsonConvert.DeserializeObject<dynamic>(userData); // Deserializa el userData
            int funcionarioId = user.Id; // Asumiendo que el ID está dentro de userData
            string nombre = user.Nombre;
            string mensaje = $"{nombre} ha tomado el reporte con ID {dto.ReporteId}";
            try
            {
                // Aquí asumimos que tienes un servicio que te permite actualizar el historial y la notificación
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["ApiUrls:GestionReportes"]);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var token = HttpContext.Session.GetString("Token");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Preparar los datos para enviar al servidor
                    var data = new
                    {
                        ReporteId = dto.ReporteId,
                        FuncionarioId = funcionarioId,
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");

                    // Realizar la solicitud para actualizar el historial
                    var response = await client.PostAsync($"api/Reporte/atender-reporte", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Llamar al método de notificación (Este método debe estar en algún servicio)
                        await GenerarNotificacion(dto.ReporteId, mensaje);
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al actualizar el reporte" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        private async Task<ActionResult> GenerarNotificacion(int reporteId, string mensaje)
        {
            // Aquí generas una notificación, el código depende de cómo implementes las notificaciones
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_configuration["ApiUrls:GestionNotificaciones"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var token = HttpContext.Session.GetString("Token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var data = new
                {
                    idReporte = reporteId,
                    mensaje = mensaje
                };

                var content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Notificaciones", content);
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Error al generar notificacion" });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarReporte(ActualizarReporte dto)
        {
            try
            {
                var userData = HttpContext.Session.GetString("UserData");
                if (userData == null)
                    return RedirectToAction("Index", "Login");
                var user = JsonConvert.DeserializeObject<dynamic>(userData); // Deserializa el userData
                int funcionarioId = user.Id; // Asumiendo que el ID está dentro de userData
                string nombre = user.Nombre;
                string mensaje = $"{nombre} ha actualizado el reporte con el siguiente mensaje: \n  {dto.comentario}";
                // Crear el objeto que se enviará a la API
                var data = new
                {
                    ReporteId = dto.ReporteId,
                    FuncionarioId = funcionarioId,
                    EstadoId = dto.estado,
                    Observacion = dto.comentario.ToString()
                };

                // Configurar HttpClient para conectarse a la API de GestionReportes
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["ApiUrls:GestionReportes"]); // Base URL de la API
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var token = HttpContext.Session.GetString("Token");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    // Serializar el objeto data en JSON
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    // Llamar al endpoint de la API
                    var response = await client.PostAsync($"api/Reporte/actualizar-reporte", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await GenerarNotificacion(dto.ReporteId, mensaje);
                        return Json(new { success = true, message = "Cambios guardados correctamente." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al guardar los cambios." });
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar errores
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
    public class AtenderReporteDTO
    {
        public int ReporteId { get; set; }
    }
    public class ActualizarReporte
    {
        public int ReporteId { get; set; }
        public int estado { get; set; }
        public string estadoNombre { get; set; }
        public string comentario { get; set; }
    }
}
