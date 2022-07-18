using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
namespace METARcord
{
    class Commands : DSharpPlus.CommandsNext.BaseCommandModule
    {
        [Command("callmetar"), Description("Calls a METAR report from a given airfield")]
        public async Task CallMetar(DSharpPlus.CommandsNext.CommandContext e, string ICAO)
        {
            try
            {   
                System.Net.WebRequest request = System.Net.WebRequest.Create($"https://www.aviationweather.gov/adds/dataserver_current/httpparam?datasource=metars&requestType=retrieve&format=xml&mostRecentForEachStation=constraint&hoursBeforeNow=1&stationString={ICAO}&fields=raw_text");
                request.ContentType = "application/xml";
                request.Method = "GET";
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                string xml = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
				Classes.FullXml data = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.FullXml>(Newtonsoft.Json.JsonConvert.SerializeXmlNode(doc));
                System.Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
                await e.RespondAsync(data.response.data.METAR.raw_text);
            }
            catch(System.Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }
    }
}