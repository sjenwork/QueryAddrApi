using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Net.Security;
using System.Security.Authentication;

namespace QueryAddrApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueryAddrController : ControllerBase
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _templatePath;


        public QueryAddrController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _templatePath = Path.Combine(env.ContentRootPath, "Configurations", "tgosSoaps.xml");

        }


        [HttpGet]
        public async Task<IActionResult> GetAddress([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address is required.");
            }

            string result = await CallQueryAddr(address);
            if (string.IsNullOrEmpty(result))
            {
                return NotFound("No address found.");
            }

            QueryAddrResult? addressList = GetQueryAddrResult(result);
            return Ok(addressList);
        }

        private async Task<string> CallQueryAddr(string address)
        {
            var serviceConfig = _configuration.GetSection("TgosQueryAddrService") ?? throw new InvalidOperationException("Configuration section 'TgosQueryAddrService' is missing.");
            string serviceUrl = serviceConfig["ServiceUrl"] ?? throw new InvalidOperationException("ServiceUrl is not configured.");


            var parameters = new
            {
                oAPPId = serviceConfig["oAPPId"],
                oAPIKey = serviceConfig["oAPIKey"],
                oAddress = address,
                oSRS = serviceConfig["oSRS"],
                oFuzzyType = serviceConfig.GetValue<int>("oFuzzyType"),
                oResultDataType = serviceConfig["oResultDataType"],
                oFuzzyBuffer = serviceConfig.GetValue<int>("oFuzzyBuffer"),
                oIsOnlyFullMatch = serviceConfig["oIsOnlyFullMatch"],
                oIsSupportPast = serviceConfig["oIsSupportPast"],
                oIsShowCodeBase = serviceConfig["oIsShowCodeBase"],
                oIsLockCounty = serviceConfig["oIsLockCounty"],
                oIsLockTown = serviceConfig["oIsLockTown"],
                oIsLockVillage = serviceConfig["oIsLockVillage"],
                oIsLockRoadSection = serviceConfig["oIsLockRoadSection"],
                oIsLockLane = serviceConfig["oIsLockLane"],
                oIsLockAlley = serviceConfig["oIsLockAlley"],
                oIsLockArea = serviceConfig["oIsLockArea"],
                oIsSameNumber_SubNumber = serviceConfig["oIsSameNumber_SubNumber"],
                oCanIgnoreVillage = serviceConfig["oCanIgnoreVillage"],
                oCanIgnoreNeighborhood = serviceConfig["oCanIgnoreNeighborhood"],
                oReturnMaxCount = serviceConfig.GetValue<int>("oReturnMaxCount")
            };

            string soapXmlTemplate = await System.IO.File.ReadAllTextAsync(_templatePath);

            string soapXml = soapXmlTemplate
                .Replace("{oAPPId}", parameters.oAPPId)
                .Replace("{oAPIKey}", parameters.oAPIKey)
                .Replace("{oAddress}", parameters.oAddress)
                .Replace("{oSRS}", parameters.oSRS)
                .Replace("{oFuzzyType}", parameters.oFuzzyType.ToString())
                .Replace("{oResultDataType}", parameters.oResultDataType)
                .Replace("{oFuzzyBuffer}", parameters.oFuzzyBuffer.ToString())
                .Replace("{oIsOnlyFullMatch}", parameters.oIsOnlyFullMatch)
                .Replace("{oIsSupportPast}", parameters.oIsSupportPast)
                .Replace("{oIsShowCodeBase}", parameters.oIsShowCodeBase)
                .Replace("{oIsLockCounty}", parameters.oIsLockCounty)
                .Replace("{oIsLockTown}", parameters.oIsLockTown)
                .Replace("{oIsLockVillage}", parameters.oIsLockVillage)
                .Replace("{oIsLockRoadSection}", parameters.oIsLockRoadSection)
                .Replace("{oIsLockLane}", parameters.oIsLockLane)
                .Replace("{oIsLockAlley}", parameters.oIsLockAlley)
                .Replace("{oIsLockArea}", parameters.oIsLockArea)
                .Replace("{oIsSameNumber_SubNumber}", parameters.oIsSameNumber_SubNumber)
                .Replace("{oCanIgnoreVillage}", parameters.oCanIgnoreVillage)
                .Replace("{oCanIgnoreNeighborhood}", parameters.oCanIgnoreNeighborhood)
                .Replace("{oReturnMaxCount}", parameters.oReturnMaxCount.ToString());

            string mediaType = "application/soap+xml";



            var httpClient = _httpClientFactory.CreateClient("QueryAddrClient");
            HttpContent httpContent = new StringContent(soapXml, Encoding.UTF8, mediaType);
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(serviceUrl, httpContent);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return await httpResponseMessage.Content.ReadAsStringAsync();
            }

            return string.Empty;
        }

        public class QueryAddrResult
        {
            public JsonArray? Info { get; set; }
            public JsonArray? AddressList { get; set; }
        }
        private QueryAddrResult? GetQueryAddrResult(string soapXmlResult)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapXmlResult);
            XmlNodeList xmlNodeList = xmlDoc.GetElementsByTagName("QueryAddrResult");
            // 檢查是否找到節點
            if (xmlNodeList == null || xmlNodeList.Count == 0 || xmlNodeList[0] == null)

            {
                // 如果沒有找到，返回空
                return null;
            }
            // 獲取 JSON 結果字符串
            string? jsonResult = xmlNodeList[0]?.InnerText;

            // 檢查 jsonResult 是否為空
            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                // 如果為空，返回空
                return null;
            }

            // 解析 JSON 結果字符串
            JsonNode? jsonNode;
            try
            {
                jsonNode = JsonNode.Parse(jsonResult);
            }
            catch (Exception ex)
            {
                // 如果解析失敗，記錄錯誤並返回空
                Console.WriteLine($"JSON 解析錯誤: {ex.Message}");
                return null;
            }

            // 檢查是否成功解析
            if (jsonNode == null)
            {
                // 如果解析失敗，返回空
                return null;
            }

            // 創建結果對象
            var result = new QueryAddrResult
            {
                Info = jsonNode["Info"] as JsonArray,
                AddressList = jsonNode["AddressList"] as JsonArray
            };

            Console.WriteLine(result);
            return result;



        }
    }
}
