using MasCercaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Net.Http.Headers;


namespace MasCercaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
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

                    // Traer todos los estados
                    var estadosResponse = await client.GetAsync("http://localhost:7780/Mantenedor/api/Mantenedor/estados-reporte");
                    List<EstadoReporte> estados = new List<EstadoReporte>();
                    if (estadosResponse.IsSuccessStatusCode)
                    {
                        estados = JsonConvert.DeserializeObject<List<EstadoReporte>>(await estadosResponse.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        ViewBag.Estados = new Dictionary<string, int>();
                        ViewBag.Error = "Error al cargar los reportes.";
                        return View();
                    }

                    // Traer todos los reportes asignados al Funcionario
                    var response = await client.GetAsync($"api/Reporte/lista-reportes?funcionarioId={funcionarioId}");
                    List<Reporte> reportes = new List<Reporte>();
                    if (response.IsSuccessStatusCode)
                    {
                        reportes = JsonConvert.DeserializeObject<List<Reporte>>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        ViewBag.Estados = new Dictionary<string, int>();
                        ViewBag.Error = "Error al cargar los reportes.";
                        return View();
                    }
                    // Contar los reportes por estado
                    var reporteCount = reportes
                        .GroupBy(r => r.Estado.nombre)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var estadosConCantidad = new Dictionary<string, int>();
                    // Asignar la cantidad de reportes a los estados
                    foreach (var estado in estados)
                    {
                        if (reporteCount.ContainsKey(estado.nombre))
                        {
                            estadosConCantidad[estado.nombre] = reporteCount[estado.nombre];
                        }
                        else
                        {
                            estadosConCantidad[estado.nombre] = 0;  // Si no hay reportes para ese estado
                        }
                    }

                    // Pasar los estados a la vista
                    ViewBag.Estados = estadosConCantidad;
                    ViewBag.Reportes = reportes;

                }
            }
            catch
            {
                ViewBag.Estados = new Dictionary<string, int>();
            }

            return View();
        }
        
        public List<SelectListItem> EstadoReportes()
        {
            // Inicializamos la lista de estados
            List<SelectListItem> estados = new List<SelectListItem>();

            try
            {
                // Hacemos la solicitud a la API para obtener los estados
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["ApiUrls:Mantenedor"]);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = client.GetAsync("api/Mantenedor/estados-reporte");

                    if (response.Result.IsSuccessStatusCode)
                    {
                        // Deserializamos la respuesta de la API
                        var result = response.Result.Content.ReadAsStringAsync();
                        var listaEstados = JsonConvert.DeserializeObject<List<EstadoReporte>>(result.Result);

                        // Convertimos la lista de EstadoReporte a SelectListItem
                        estados = listaEstados.Select(e => new SelectListItem
                        {
                            Text = e.nombre,  // El texto visible en el dropdown
                            Value = e.id.ToString()  // El valor que se enviará al servidor
                        }).ToList();

                    }
                    else
                    {
                        ViewBag.Error = "No se pudieron cargar los estados.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al cargar los estados: {ex.Message}";
            }

            // Pasamos los estados y el reporte al ViewBag
            return estados;

        }
        public async Task<IActionResult> ObtenerReporte(int id)
        {
            try
            {
                var token = HttpContext.Session.GetString("Token");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Index", "Login");
                }

                using (var client = new HttpClient())
                {
                    // Suponemos que la URL de la API externa está configurada en appsettings.json
                    client.BaseAddress = new Uri(_configuration["ApiUrls:GestionReportes"]);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Llamada GET a la API externa
                    var response = await client.GetAsync($"api/Reporte/obtener-reporte/{id}");
                    Reporte reporte = new Reporte();
                    if (response.IsSuccessStatusCode)
                    {
                        reporte = JsonConvert.DeserializeObject<Reporte>(await response.Content.ReadAsStringAsync());
                        ViewBag.EstadosRep = EstadoReportes();
                        // Si todo es correcto, puedes retornar una vista parcial o el HTML con los detalles
                        return PartialView("_DetalleReporte", reporte);  // `_DetalleReporte` es un partial view con los detalles del reporte
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se pudo obtener el reporte." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}