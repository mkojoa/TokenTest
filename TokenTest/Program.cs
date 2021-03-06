using IdentityModel.Client;
using IdentityModel.OidcClient;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TokenTest 
{

    //ConsoleClientWithBrowser
    public class Program
    {
        static OidcClient _oidcClient;

        //api
        static HttpClient _apiClient 
            = new HttpClient { BaseAddress = new Uri("http://psl-app-vm3/PayrollService.API/api/v1.1/AccessPolicies/spGetCompanyByLoggedIn") };

        public static async Task Main()
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with OIDC    |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            await SignIn();
        }

        private static async Task SignIn()
        {
            // create a redirect URI using an available port on the loopback address.
            // requires the OP to allow random ports on 127.0.0.1 - otherwise set a static port
            var browser = new SystemBrowser();

            //string redirectUri = string.Format($"http://127.0.0.1:{5000}");
            //string redirectUri = string.Format($"com.persol.hcm_mobile:/oauthredirect");
            string redirectUri = string.Format($"http://psl-app-vm3/persol-hcm?qryparam=1AEC6ADD-14FE-4427-98F5-32E00B7E72B1");// ($"http://psl-app-vm3/persol-hcm?qryparam=acc001");

            var options = new OidcClientOptions
            {
                Authority = "https://localhost:44318",

                ClientId = "angular_code_client", // "console.pkce", hcmmobilecodeflow
                RedirectUri = redirectUri,
                Scope = "profile openid email", // payrollapi globalapi hcmmanagerapi",
                FilterClaims = false,
                Browser = browser,
                Policy = new Policy
                {
                    Discovery = new DiscoveryPolicy
                    {
                        RequireHttps = false
                    }
                }
            };
            

            options.Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode;

            //var serilog = new LoggerConfiguration()
            //    .MinimumLevel.Error()
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
            //    .CreateLogger();

            //options.LoggerFactory.AddSerilog(serilog);

            _oidcClient = new OidcClient(options);

            var result = await _oidcClient.LoginAsync(new LoginRequest());

            ShowResult(result);
            await NextSteps(result);
        }

        private static void ShowResult(LoginResult result)
        {
            if (result.IsError)
            {
                Console.WriteLine("\n\nError:\n{0}", result.Error);
                return;
            }

            Console.WriteLine("\n\nClaims:");
            foreach (var claim in result.User.Claims)
            {
                Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
            }

            Console.WriteLine($"\nidentity token: {result.IdentityToken}");
            Console.WriteLine($"access token:   {result.AccessToken}");
            Console.WriteLine($"refresh token:  {result?.RefreshToken ?? "none"}");
        }

        private static async Task NextSteps(LoginResult result)
        {
            var currentAccessToken = result.AccessToken;
            var currentRefreshToken = result.RefreshToken;

            var menu = "  x...exit  c...call api   ";
            if (currentRefreshToken != null) menu += "r...refresh token   ";

            while (true)
            {
                Console.WriteLine("\n\n");

                Console.Write(menu);
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.X) return;
                if (key.Key == ConsoleKey.C) await CallApi(currentAccessToken);
                if (key.Key == ConsoleKey.R)
                {
                    var refreshResult = await _oidcClient.RefreshTokenAsync(currentRefreshToken);
                    if (result.IsError)
                    {
                        Console.WriteLine($"Error: {refreshResult.Error}");
                    }
                    else
                    {
                        currentRefreshToken = refreshResult.RefreshToken;
                        currentAccessToken = refreshResult.AccessToken;

                        Console.WriteLine("\n\n");
                        Console.WriteLine($"access token:   {result.AccessToken}");
                        Console.WriteLine($"refresh token:  {result?.RefreshToken ?? "none"}");
                    }
                }
            }
        }

        private static async Task CallApi(string currentAccessToken)
        {
            _apiClient.SetBearerToken(currentAccessToken);
            var response = await _apiClient.GetAsync("identity");

            if (response.IsSuccessStatusCode)
            {
                var json = JArray.Parse(await response.Content.ReadAsStringAsync());
                Console.WriteLine("\n\n");
                Console.WriteLine(json);
            }
            else
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");
            }
        }
    }
}