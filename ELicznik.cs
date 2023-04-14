using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ELicznikScraper
{
    public class ELicznik
    {
        private readonly HttpClient _httpClient;
        private string _username;
        private string _password;

        public ELicznik(string username, string password)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer(), 
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            _httpClient = new HttpClient(handler);
            _username = username;
            _password = password;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        }

        ~ELicznik() 
        {
             SignOut();
        }

        public async Task<bool> ChangeUser(string username, string password)
        {
            SignOut();
            _username = username;
            _password = password;
            return await SignIn();
        }

        private async Task<bool> SignIn()
        {
            var loginPageRequest = new HttpRequestMessage(HttpMethod.Get, "https://logowanie.tauron-dystrybucja.pl/login");
            await _httpClient.SendAsync(loginPageRequest);

            var loginRequest = new HttpRequestMessage(HttpMethod.Post, $"https://logowanie.tauron-dystrybucja.pl/login?username={_username}&password={_password}");
            
            //response sets session cookie on logowanie.tauron-dystrybucja.pl domain
            var loginResponse = await _httpClient.SendAsync(loginRequest);

            if (!loginResponse.IsSuccessStatusCode)
            {
                return false;
            }

            //connect to e-licznik service and set authentication cookies on elicznik.tauron-dystrybucja.pl domain
            var serviceRequest = new HttpRequestMessage(HttpMethod.Get, "https://logowanie.tauron-dystrybucja.pl/extranets/2/connect");
            
            var serviceResponse = await _httpClient.SendAsync(serviceRequest);
            
            if (!serviceResponse.IsSuccessStatusCode) 
            {
                return false;
            }

            return true;
        }

        private bool SignOut()
        {
            var logoutRequest = new HttpRequestMessage(HttpMethod.Get, $"https://elicznik.tauron-dystrybucja.pl/applogout");

            //response sets session cookie on logowanie.tauron-dystrybucja.pl domain
            var logoutResponse = _httpClient.Send(logoutRequest);

            if (logoutResponse.StatusCode != HttpStatusCode.Found)
            {
                return false;
            }
            return true;
        }

        public async Task<string> GetData(DateOnly from, DateOnly to, DataFrequency dataFrequency = DataFrequency.Hourly, FileType fileType = FileType.CSV, DataScope dataScope = DataScope.ConsumptionOnly)
        {
            //check if user is signed in and re-logs him if not
            if (! await CheckIfSessionIsAlive())
            {
                await SignIn();

                if (! await CheckIfSessionIsAlive())
                {
                    throw new WebException("Unable to sign in");
                }
            }

            var dataRequest = new HttpRequestMessage(HttpMethod.Post, "https://elicznik.tauron-dystrybucja.pl/energia/do/dane");


            var dateFrom = from switch
            {
                DateOnly d when d < DateOnly.FromDateTime(DateTime.Today) && d > new DateOnly(1970,01,01) && d < to => from,
                _ => DateOnly.FromDateTime(DateTime.Today)
            };

            var dateTo = to switch
            {
                DateOnly d when d < DateOnly.FromDateTime(DateTime.Today) && d > new DateOnly(1970, 01, 01) && d > from => to,
                _ => DateOnly.FromDateTime(DateTime.Today)
            };

            var dataFrequencyType = dataFrequency switch
            {
                DataFrequency.Daily => "dzien",
                _ => "godzin"
            };



            var dataRetrieveParameters = new List<KeyValuePair<string, string>>
            {
            new KeyValuePair<string, string>("form[from]", dateFrom.ToString("dd.MM.yyyy")),
            new KeyValuePair<string, string>("form[to]", dateTo.ToString("dd.MM.yyyy")),
            new KeyValuePair<string, string>("form[type]", dataFrequencyType),
            new KeyValuePair<string, string>("form[fileType]", "CSV")
            };

            if (dataScope != DataScope.ConsumptionOnly)
            {
                dataRetrieveParameters.Add(new KeyValuePair<string, string>("form[oze]", "1"));
            }

            if (dataScope != DataScope.ProductionOnly)
            {
                dataRetrieveParameters.Add(new KeyValuePair<string, string>("form[consum]", "1"));
            }

            //passing data type as form parameters
            var dataRetrieveContent = new FormUrlEncodedContent(dataRetrieveParameters);
            dataRequest.Content = dataRetrieveContent;

            var dataResponse = await _httpClient.SendAsync(dataRequest);

            if (dataResponse.Content.Headers.ContentType.MediaType.Equals("text/html"))
            {
                if (!await SignIn())
                {
                    throw new WebException("Unable to sign in");
                }
                var repeatedRequest = 
                dataResponse = await _httpClient.SendAsync(dataRequest);

                if (dataResponse.Content.Headers.ContentType.MediaType.Equals("text/html"))
                {
                    throw new WebException("Unable to sign in");
                }
            }


            if (fileType == FileType.CSV)
            {
                return await dataResponse.Content.ReadAsStringAsync();
            }

            //add parsing to json here
            return await dataResponse.Content.ReadAsStringAsync();
        }

        public async Task<string> GetData()
        {
            return await GetData(DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), DateOnly.FromDateTime(DateTime.Now));
        }

        private async Task<bool> CheckIfSessionIsAlive()
        {
            var dataRequest = new HttpRequestMessage(HttpMethod.Post, "https://elicznik.tauron-dystrybucja.pl/energia/do/dane");

            var dataRetrieveParameters = new List<KeyValuePair<string, string>>
            {
            new KeyValuePair<string, string>("form[from]", "01.01.1971"),
            new KeyValuePair<string, string>("form[to]", "02.01.1971"),
            new KeyValuePair<string, string>("form[type]", "godzin"),
            new KeyValuePair<string, string>("form[consum]", "1"),
            new KeyValuePair<string, string>("form[fileType]", "CSV")
            };

            //passing data type as form parameters
            var dataRetrieveContent = new FormUrlEncodedContent(dataRetrieveParameters);
            dataRequest.Content = dataRetrieveContent;

            var dataResponse = await _httpClient.SendAsync(dataRequest);

            if (dataResponse.Content.Headers.ContentType.MediaType.Equals("text/csv"))
            {
                return true;
            }
            return false;
        }

    }
}
