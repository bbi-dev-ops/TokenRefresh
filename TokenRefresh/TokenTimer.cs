using System;
using System;
using System.Xml;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace TokenTimer
{
    public static class TokenTimer
    {
        [FunctionName("Febreze")]
        public static void Run([TimerTrigger("0 */13 * * * *")] TimerInfo myTimer, ILogger log)
        {

            var client = new RestClient("http://cnx.dat.com:8000/TfmiRequest");
            client.Timeout = -1;
            var request1 = new RestRequest(Method.POST);
            string body1 = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tcor=""http://www.tcore.com/TcoreHeaders.xsd"" xmlns:tcor1=""http://www.tcore.com/TcoreTypes.xsd"" xmlns:tfm=""http://www.tcore.com/TfmiFreightMatching.xsd"">
                            <soapenv:Header>
                                <tcor:sessionHeader soapenv:mustUnderstand=""1"">
                                    <tcor:sessionToken>
                                        <tcor1:primary/>
                                        <tcor1:secondary/>
                                    </tcor:sessionToken>
                                </tcor:sessionHeader>
                                <tcor:correlationHeader soapenv:mustUnderstand=""0"">
                                    <!--Optional:-->
                                    <tcor:Id/>
                                </tcor:correlationHeader>
                                <tcor:applicationHeader soapenv:mustUnderstand=""0"">
                                    <tcor:application>BBI</tcor:application>
                                    <tcor:applicationVersion>2020</tcor:applicationVersion>
                                </tcor:applicationHeader>
                            </soapenv:Header>
                            <soapenv:Body>
                                <tfm:loginRequest>
                                    <tfm:loginOperation>
                                        <tfm:loginId>bbiwildcat</tfm:loginId>
                                        <tfm:password>dat123</tfm:password>
                                        <tfm:thirdPartyId>BBI</tfm:thirdPartyId>
                                        <!--Optional:-->
                                        <tfm:apiVersion>2</tfm:apiVersion>
                                    </tfm:loginOperation>
                                </tfm:loginRequest>
                            </soapenv:Body>
                        </soapenv:Envelope>";

            request1.AddHeader("Content-Type", "text/xml");
            //request.AddParameter("text/xml", "<soapenv:Envelope xmlns:soapenv=\http://schemas.xmlsoap.org/soap/envelope/\ xmlns:tcor=\http://www.tcore.com/TcoreHeaders.xsd\ xmlns:tcor1=\http://www.tcore.com/TcoreTypes.xsd\ xmlns:tfm=\http://www.tcore.com/TfmiFreightMatching.xsd\>\n    <soapenv:Header>\n        <tcor:sessionHeader soapenv:mustUnderstand=\"1\">\n            <tcor:sessionToken>\n                <tcor1:primary/>\n                <tcor1:secondary/>\n            </tcor:sessionToken>\n        </tcor:sessionHeader>\n        <tcor:correlationHeader soapenv:mustUnderstand=\"0\">\n            <!--Optional:-->\n            <tcor:Id/>\n        </tcor:correlationHeader>\n        <tcor:applicationHeader soapenv:mustUnderstand=\"0\">\n            <tcor:application>bbi</tcor:application>\n            <tcor:applicationVersion>test1</tcor:applicationVersion>\n        </tcor:applicationHeader>\n    </soapenv:Header>\n    <soapenv:Body>\n        <tfm:loginRequest>\n            <tfm:loginOperation>\n                <tfm:loginId>bbosse</tfm:loginId>\n                <tfm:password>bbi330</tfm:password>\n                <tfm:thirdPartyId>BBI</tfm:thirdPartyId>\n                <!--Optional:-->\n                <tfm:apiVersion>2</tfm:apiVersion>\n            </tfm:loginOperation>\n        </tfm:loginRequest>\n    </soapenv:Body>\n</soapenv:Envelope>", ParameterType.RequestBody);
            request1.AddParameter("text/xml", body1, ParameterType.RequestBody);
            IRestResponse response1 = client.Execute(request1);
            Console.WriteLine(response1.Content);

            XmlDocument xmlDoc1 = new XmlDocument();
            xmlDoc1.LoadXml(response1.Content);
            string responseText1 = JsonConvert.SerializeXmlNode(xmlDoc1);
            JToken token1 = JObject.Parse(responseText1);

            //string toke = token.SelectToken("soapenv:Envelope.soapenv:Body.tfm:loginResponse.tfm:loginResult.tfm:loginSuccessData.tfm:token.tcor:primary").ToString();
            //JToken xrs = JObject.Parse(toke);

            //TempData["CheckCode"] = "SUCCESS";

            var RealmId = (string)token1.SelectToken("soapenv:Envelope.soapenv:Body.tfm:loginResponse.tfm:loginResult.tfm:loginSuccessData.tfm:token.tcor:primary.#text");
            var Access_Token = (string)token1.SelectToken("soapenv:Envelope.soapenv:Body.tfm:loginResponse.tfm:loginResult.tfm:loginSuccessData.tfm:token.tcor:secondary.#text");
            DateTime exp = (DateTime)token1.SelectToken("soapenv:Envelope.soapenv:Body.tfm:loginResponse.tfm:loginResult.tfm:loginSuccessData.tfm:expiration");
            var Refresh_Token = exp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

            var azureClient = new SecretClient(vaultUri: new Uri("https://devocelotkv.vault.azure.net/"), credential: new DefaultAzureCredential());

            azureClient.SetSecret("RealmId", RealmId);
            azureClient.SetSecret("AccessToken", Access_Token);
            azureClient.SetSecret("RefreshToken", Refresh_Token);

        }
    }
}
