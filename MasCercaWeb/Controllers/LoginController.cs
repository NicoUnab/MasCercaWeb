using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MasCercaWeb.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
                                                    .AddJsonFile("appsettings.json")
                                                    .AddEnvironmentVariables()
                                                    .Build();

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate(string rut, string contraseña)
        {
            int newrut = 0;
            int.TryParse(rutLogin(rut), out newrut);
            var loginData = new
            {
                rut = newrut,
                contraseña = contraseña,
                tipoAplicacion = "Web"
            };

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_configuration["ApiUrls:GestionUsuarios"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<LoginResponse>(await response.Content.ReadAsStringAsync());
                    HttpContext.Session.SetString("Token", result.Token);
                    HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Usuario));

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Credenciales inválidas";
                    return View("Index");
                }
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public string rutLogin(string rut) 
        {
            rut= rut.Replace(".", "").Replace("-", "");
            if (rut.Length < 2) return rut;
            string body = rut.Substring(0, rut.Length - 1);
            return $"{body}";
        }
    }
    public class LoginResponse
    {
        public Usuario Usuario { get; set; }
        public string Token { get; set; }
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }
}

