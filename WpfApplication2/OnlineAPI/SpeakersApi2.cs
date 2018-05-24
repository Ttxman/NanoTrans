using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using TranscriptionCore;

namespace NanoTrans.OnlineAPI
{
    public class SpeakersApi2 : SpeakersApi
    {

        //TODO: check errors in post& get methods

        public SpeakersApi2(string url, Window owner): base(url, owner)
        {

        }

        public override async Task<IEnumerable<ApiSynchronizedSpeaker>> SimpleSearch(string _filterstring)
        {
            var apiurl = new Uri(Info.SpeakerAPI_URL, @"?call=search");
            var data = new JObject();
            data.Add("text", _filterstring);
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json);
            return ParseSpeakers(jo).ToArray();
        }

        public override IEnumerable<ApiSynchronizedSpeaker> ParseSpeakers(JObject json)
        {
            var jspeakers = json.GetValue("result") as JArray;

            foreach (var item in jspeakers)
            {
                yield return ParseSpeaker(item as JObject);
            }
        }

        protected override ApiSynchronizedSpeaker ParseSpeaker(JObject item)
        {
            var result = item.GetValue("result");
            if (result != null)
                item = result as JObject;

            var itm = item["id"];
            item.Remove("id");
            item.Add("dbid", itm);
            item.Remove("update");

            string sex = item["sex"].Value<string>();
            item["sex"] = (sex == "m" || sex.ToLower() == "male") ? "Male" : (sex == "f" || sex.ToLower() == "female") ? "Female" : "X";
            item["defaultlang"] = item["lang"];

            item.Remove("lang");

            var s = item.ToObject<ApiSynchronizedSpeaker>();
            s.Synchronized = DateTime.Now;
            s.DataBaseType = DBType.Api;

            return s;
        }

        public override async Task<IEnumerable<ApiSynchronizedSpeaker>> ListSpeakers(IEnumerable<string> guids)
        {
            var apiurl = new Uri(Info.SpeakerAPI_URL, @"?call=getSpeakers");
            var data = new JObject();
            data.Add("id", new JArray(guids.ToArray()));
            var cont = await PostAsync(apiurl, data);

            if (cont.StatusCode != HttpStatusCode.OK)
            {
                MessageBox.Show("API Error", "Error", MessageBoxButton.OK);
                return null;
            }


            string json = await cont.Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);

            return ParseSpeakers(jo).ToArray(); ;
        }

        public override async Task<ApiSynchronizedSpeaker> GetSpeaker(string p)
        {

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"?call=getSpeaker");
            var data = new JObject();
            data.Add("id", p);
            data.Add("attributes", true);
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);
            return ParseSpeaker(jo);
        }



        internal override async Task<bool> UpdateSpeaker(ApiSynchronizedSpeaker speaker)
        {
            var apiurl = new Uri(Info.SpeakerAPI_URL, @"?call=updateSpeaker");
            var data = SerializeSpeaker(speaker);
            var resp = await PostAsync(apiurl, data);
            return true;
        }

        internal override async Task<bool> AddSpeaker(ApiSynchronizedSpeaker speaker)
        {
            var apiurl = new Uri(Info.SpeakerAPI_URL, @"?call=setSpeaker");
            var data = SerializeSpeaker(speaker);
            data.Remove("id");
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json);

            speaker.DBID = jo["result"].ToString();
            speaker.IsSaved = true;

            return true;
        }


        protected override JObject SerializeSpeaker(Speaker speaker)
        {

            speaker.Synchronized = DateTime.Now;
            speaker.DataBaseType = DBType.Api;
            var s = JObject.FromObject(speaker);
            s.Remove("mainid");
            s.Remove("dbtype");
            s.Remove("merges");//TODO: implement merging on server side
            s.Remove("dbtype");
            s.Remove("databasetype");
            s.Remove("id");
            s.Remove("elements");
            s.Remove("idfixed");
            s.Remove("fullname");
            s.Remove("pinnedtodocument");
            s.Remove("synchronized");

            var id = s["dbid"];
            s.Remove("dbid");
            s.Add("id", id);

            s["sex"] = (speaker.Sex == Speaker.Sexes.Male) ? "m" : (speaker.Sex == Speaker.Sexes.Female) ? "f" : "x";
            s["lang"] = s["defaultlang"];
            s.Remove("defaultlang");


            if (s["degreebefore"] != null)
            {
                s["degreeBefore"] = s["degreebefore"];
                s.Remove("degreebefore");
            }

            if (s["degreeafter"] != null)
            {
                s["degreeAfter"] = s["degreeafter"];
                s.Remove("degreeafter");
            }
            return s;
        }

        internal override async Task<bool> UploadTranscription(WPFTranscription Transcription)
        {
            if (!LogedIn)
                await TryLogin();

            MemoryStream ms = new MemoryStream();
            var wr = new XmlTextWriter(ms, Encoding.UTF8);
            Transcription.Serialize().WriteTo(wr);
            wr.Flush();
            var trsx = new ByteArrayContent(ms.ToArray());
            ms.Seek(0, SeekOrigin.Begin);
            trsx.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            var hm = await _client.PostAsync(Info.TrsxUploadURL, trsx);

            if (!(hm.StatusCode == System.Net.HttpStatusCode.Created || hm.StatusCode == System.Net.HttpStatusCode.OK))
            {
                MessageBox.Show("Upload Failed", "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show("File was sucessfully uploaded", "File was sucessfully uploaded", MessageBoxButton.OK, MessageBoxImage.Information);


            return true;
     
        }
    }
}
