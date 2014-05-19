using NanoTrans.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace NanoTrans.OnlineAPI
{
    /// <summary>
    /// implementation for
    /// -urlapi "https://senatarchiv.tul.cz/senat/nanotrans/configuration?documentId=12&site=https://senatarchiv.tul.cz/senat"
    /// </summary>
    
    public class SpeakersApi
    {
        public Uri Url { get; private set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        HttpClient _client;
        HttpClientHandler _handler;
        CancellationTokenSource _abortSource = new CancellationTokenSource();

        public SpeakersApi(string url, Window owner)
        {
            Url = new Uri(url);
            _handler = new HttpClientHandler();
            _client = new HttpClient(_handler);
            _client.Timeout = TimeSpan.FromSeconds(10);
            _handler.CookieContainer = new System.Net.CookieContainer();
            _handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;


            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.ContractResolver = new LowercaseContractResolver();
                return settings;
            });
        }

        private class LowercaseContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToLower();
            }
        }

        public void Cancel()
        {
            _client.CancelPendingRequests();
            _abortSource.Cancel();
            _abortSource = new CancellationTokenSource();
        }


        public async Task<T> GetUrlToJson<T>(Uri url)
        {
            string json = await _client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(json);
        }


        public async Task<HttpResponseMessage> GetUrl(Uri url, params Tuple<string, string>[] values)
        {
            if (values.Length > 0)
                url = new Uri(url, "?" + string.Join("&", values.Select(v => v.Item1 + "=" + v.Item2)));

            return await _client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri url, JObject data)
        {
            return await PostAsync(url, data.ToString());
        }


        public async Task<HttpResponseMessage> PostAsync(Uri url, XDocument data)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("content",data.ToString(SaveOptions.DisableFormatting)),
                new KeyValuePair<string, string>("documentId",data.ToString(SaveOptions.DisableFormatting)),
            });

            content.Headers.ContentType.MediaType = "application/xml";
            return await PostAsync(url, content);
        }


        public async Task<HttpResponseMessage> PostAsync(Uri url, HttpContent content)
        { 
            var r = await _client.PostAsync(url, content, _abortSource.Token);

            if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TryLogin(_owner);
            }

            return r;
        }



        public async Task<HttpResponseMessage> PostAsync(Uri url, string data)
        {
            var content = new StringContent(data);
            content.Headers.ContentType.MediaType = "application/json";
            return await PostAsync(url, content);
        }


        public async Task<IEnumerable<ApiSynchronizedSpeaker>> ListSpeakers(IEnumerable<string> guids)
        {
            var apiurl = new Uri(Info.SpeakersAPI,@"speaker/list");
            var data = new JObject();
            data.Add("ids", new JArray(guids.ToArray()));
            string json = await (await PostAsync(apiurl, data)).Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);

            return ParseSpeakers(jo).ToArray();;
        }

        public async Task<ApiSynchronizedSpeaker> GetSpeaker(string p)
        {
            var apiurl = new Uri(Info.SpeakersAPI, @"speaker/get");
            var data = new JObject();
            data.Add("speakerId", new JArray(p));
            var resp = await PostAsync(apiurl, data);
            string json = await(resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json).GetValue("speaker");
            return ParseSpeaker(jo);
        }


        public async Task<IEnumerable<ApiSynchronizedSpeaker>> SimpleSearch(string _filterstring)
        {
            var apiurl = new Uri(Info.SpeakersAPI, @"speaker/simpleSearch");
            var data = new JObject();
            data.Add("text",  _filterstring);
            var resp = await PostAsync(apiurl, data);
            string json = await(resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json);
            return ParseSpeakers(jo).ToArray();
        }

        internal async Task UpdateSpeaker(Speaker speaker)
        {
            var apiurl = new Uri(Info.SpeakersAPI, @"/speaker/edit");
            var data = SerializeSpeaker(speaker);
            var resp = await PostAsync(apiurl, data);
            string json = await(resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json);
        }


        public IEnumerable<ApiSynchronizedSpeaker> ParseSpeakers(JObject json)
        {
            var jspeakers = json.GetValue("speakers") as JArray;
            foreach (var item in jspeakers)
            {
                yield return ParseSpeaker(item as JObject); 
            }
        }

        private JObject SerializeSpeaker(Speaker speaker)
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
            s.Add("speakerId", id);

            s["sex"] = (speaker.Sex == Speaker.Sexes.Male) ? "MALE" : (speaker.Sex == Speaker.Sexes.Female) ? "FEMALE" : "UNKNOWN";
            s["lang"] = s["defaultlang"];
            s.Remove("defaultlang");
            
            //var itm = item["id"];
            //item.Remove("id");
            //item.Add("dbid", itm);
            //item.Remove("update");

            //string sex = item["sex"].Value<string>();
            //item["sex"] = (sex == "m" || sex.ToLower() == "male") ? "Male" : (sex == "f" || sex.ToLower() == "female") ? "Female" : "X";

            
            return s;
        }

        private ApiSynchronizedSpeaker ParseSpeaker(JObject item)
        {
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

        public bool TryLogin(Window owner)
        {
            var w = new OnlineTranscriptionWindow(this);
            w.Owner = owner;

            return w.ShowDialog() == true;
        }

        public WPFTranscription Trans { get; set; }

        internal async Task LoadInfo()
        {
            string json = await _client.GetStringAsync(Url);
            _info = JsonConvert.DeserializeObject<OnlineTranscriptionInfo>(json);
        }

        OnlineTranscriptionInfo _info;

        public OnlineTranscriptionInfo Info
        {
            get { return _info; }
            set { _info = value; }
        }

        public async Task Login()
        {
            var resultMessage = await GetUrl(Info.LoginURL, Tuple.Create("username", UserName), Tuple.Create("password", Password));
            var data = await resultMessage.Content.ReadAsStringAsync();
            var resultJson = JObject.Parse(data);

            CheckForErrors(resultJson);

            if (!(bool)resultJson["valid"])
                throw new ApiException("Invalid login");

            LogedIn = true;
        }

        private void CheckForErrors(JObject resultJson)
        {
            if (resultJson["actionErrors"].HasValues || resultJson["fieldErrors"].HasValues)
            {
                throw new ApiException(resultJson.ToString());
            }
        }

        private bool _logedIn = false;

        public bool LogedIn
        {
            get { return _logedIn; }
            set { _logedIn = value; }
        }

        internal async Task<bool> UploadTranscription(WPFTranscription Transcription)
        {
            HttpResponseMessage hm;
            try
            {
               // var wind = new OnlineTranscriptionWindow(this) { Owner = this._owner };
                hm = await PostAsync(_info.ResponseURL, Transcription.Serialize());
                return true;
            }
            catch
            { }
            return false;
        }

        public Window _owner { get; set; }

        public CancellationToken CancelationToken {
            get
            {
                return _abortSource.Token;
            }
        }



    }
}
