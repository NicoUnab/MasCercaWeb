namespace MasCercaWeb.Services
{
    public class GestionUsuarios
    {
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
                                                    .AddJsonFile("appsettings.json")
                                                    .AddEnvironmentVariables()
                                                    .Build();
    }
}
