using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using TranscriptionCore;

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

        protected HttpClient _client;
        protected HttpClientHandler _handler;
        protected CancellationTokenSource _abortSource = new CancellationTokenSource();

        public SpeakersApi(string url, Window owner)
        {
            Url = new Uri(url);
            _handler = new HttpClientHandler();
            _client = new HttpClient(_handler);
            _client.Timeout = TimeSpan.FromMinutes(1);
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


        public async Task<HttpResponseMessage> PostAsync(Uri url, HttpContent content)
        {
            var r = await _client.PostAsync(url, content, _abortSource.Token);

            if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new NotImplementedException();
                await TryLogin();
            }

            return r;
        }



        public async Task<HttpResponseMessage> PostAsync(Uri url, string data)
        {
            var content = new StringContent(data);
            content.Headers.ContentType.MediaType = "application/json";
            return await PostAsync(url, content);
        }


        public virtual async Task<IEnumerable<ApiSynchronizedSpeaker>> ListSpeakers(IEnumerable<string> guids)
        {
            if (!LogedIn)
                await TryLogin();

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/speaker/list");
            var data = new JObject();
            data.Add("ids", new JArray(guids.ToArray()));
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

        public virtual async Task<ApiSynchronizedSpeaker> GetSpeaker(string p)
        {
            if (!LogedIn)
                await TryLogin();

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/speaker/get");
            var data = new JObject();
            data.Add("id", p);
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);
            return ParseSpeakers(jo).First();
        }

        internal virtual async Task<bool> UpdateSpeaker(ApiSynchronizedSpeaker speaker)
        {
            if (!LogedIn)
                await TryLogin();

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/speaker/edit");
            var data = SerializeSpeaker(speaker);
            var resp = await PostAsync(apiurl, data);

            if (resp.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                MessageBox.Show("Update Failed", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }


        internal virtual async Task<bool> AddSpeaker(ApiSynchronizedSpeaker speaker)
        {
            if (!LogedIn)
                await TryLogin();

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/speaker/add");
            var data = SerializeSpeaker(speaker);
            data.Remove("id");
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = JObject.Parse(json);

            if (jo["id"] is null)
            {
                MessageBox.Show("save faled", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            speaker.DBID = jo["id"].ToObject<string>();
            speaker.IsSaved = true;

            return true;
        }


        public virtual async Task<IEnumerable<ApiSynchronizedSpeaker>> SimpleSearch(string _filterstring)
        {
            if (!LogedIn)
                await TryLogin();

            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/speaker/simpleSearch");
            var data = new JObject();
            data.Add("text", _filterstring);
            var resp = await PostAsync(apiurl, data);
            string json = await (resp).Content.ReadAsStringAsync();
            var jo = (JObject)JObject.Parse(json);
            return ParseSpeakers(jo).ToArray();
        }

        public virtual IEnumerable<ApiSynchronizedSpeaker> ParseSpeakers(JObject json)
        {
            var jspeakers = json.GetValue("speakers") as JArray;
            foreach (var item in jspeakers)
            {
                yield return ParseSpeaker(item as JObject);
            }
        }

        protected virtual JObject SerializeSpeaker(Speaker speaker)
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

            s["sex"] = (speaker.Sex == Speaker.Sexes.Male) ? "MALE" : (speaker.Sex == Speaker.Sexes.Female) ? "FEMALE" : "UNKNOWN";
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

        protected virtual ApiSynchronizedSpeaker ParseSpeaker(JObject item)
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


        public virtual void CheckLogin()
        {
            // Application.Current.MainWindow.Dispatcher.Invoke();
        }

        public async Task<bool> TryLogin()
        {

            var owner = Application.Current.MainWindow;
            if (Info is null)
                await this.LoadInfo();

            if (Info.API2)
            {
                if (this is SpeakersApi2)
                    await DownloadTranscription();
                return true;
            }
            var w = new OnlineTranscriptionWindow(this);
            w.Owner = owner;

            return (w.ShowDialog() == true);
        }

        public WPFTranscription Trans { get; set; }

        internal async Task LoadInfo()
        {
            string json = await _client.GetStringAsync(Url);
            _info = JsonConvert.DeserializeObject<OnlineTranscriptionInfo>(json);

            if (_info.TrsxDownloadURL is null)
            {
                var data = JObject.Parse(json);
                if (data["result"] is { } dres) //API2 .. i should separate them better
                {
                    _info = JsonConvert.DeserializeObject<OnlineTranscriptionInfo>(dres.ToString());
                    _info.API2 = true;
                }
            }

            _info.OriginalURL = Url;
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
            LogedIn = true;
        }

        private void CheckForErrors(JObject resultJson)
        {
            if (resultJson["valid"] is { } valid && !valid.ToObject<bool>())
            {
                throw new ApiException(resultJson.ToString());
            }
        }

        private bool _logedIn = false;

        public bool LogedIn
        {
            get { return _logedIn || (Info != null && Info.API2); }
            set { _logedIn = value; }
        }

        internal virtual async Task<bool> UploadTranscription(WPFTranscription Transcription)
        {
            if (!LogedIn)
                await TryLogin();

            var cont = new MultipartFormDataContent();

            MemoryStream ms = new MemoryStream();
            var wr = new XmlTextWriter(ms, Encoding.UTF8);
            Transcription.Serialize().WriteTo(wr);
            wr.Flush();
            var filename = Path.GetFileName(Transcription.DocumentID + ".trsx");
            var trsx = new ByteArrayContent(ms.ToArray());
            ms.Seek(0, SeekOrigin.Begin);
            trsx.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            cont.Add(trsx, "file", filename);
            cont.Add(new StringContent(filename), "name");

            var hm = await _client.PostAsync(Info.TrsxUploadURL, cont);

            var resp = JObject.Parse(await hm.Content.ReadAsStringAsync());

            if (hm.StatusCode != System.Net.HttpStatusCode.Created)
            {
                MessageBox.Show("Upload Failed", "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }


            var apiurl = new Uri(Info.SpeakerAPI_URL, @"v1/document/retranscribe");

            var data = new JObject();
            data["id"] = Transcription.DocumentID;
            data["transcriptFileId"] = resp["id"];

            hm = await PostAsync(apiurl, data);

            if (hm.StatusCode != HttpStatusCode.Accepted)
            {
                MessageBox.Show("Upload Failed", "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show("File was sucessfully uploaded", "File was sucessfully uploaded", MessageBoxButton.OK, MessageBoxImage.Information);


            return true;
        }

        public Window _owner { get; set; }

        public CancellationToken CancelationToken
        {
            get
            {
                return _abortSource.Token;
            }
        }

        internal async Task DownloadFile(string RequestURL, string Filename)
        {
            var resp = await _client.GetAsync(RequestURL);

            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show("Error occured during audio download", "Error occured during audio download", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var fileStream = new FileStream(Filename, FileMode.Create, FileAccess.Write);
            await resp.Content.CopyToAsync(fileStream);
        }

        internal void LoadTranscription(WPFTranscription transcription)
        {
            Trans = transcription;
            Trans.IsOnline = true;
            Trans.Api = this;
            Trans.Meta.Add(JsonConvert.DeserializeXNode(JsonConvert.SerializeObject(Info), "OnlineInfo").Root);
        }

        internal async Task UpdateTranscriptionSpeakers()
        {
            var sp1 = await ListSpeakers(Trans.Speakers.Where(s => s.DBType == DBType.Api).Select(s => s.DBID));
            var respeakers = sp1.ToArray();

            for (int i = 0; i < respeakers.Length; i++)
            {
                var replacement = respeakers[i];
                var dbs = Trans.Speakers.GetSpeakerByDBID(replacement.DBID);
                if (dbs != null)
                {
                    replacement.PinnedToDocument = dbs.PinnedToDocument | replacement.PinnedToDocument;
                    if (replacement.MainID != null)
                        replacement.DBID = replacement.MainID;

                    Trans.ReplaceSpeaker(dbs, respeakers[i]);
                }
            }
        }

        internal async Task DownloadTranscription()
        {
            HttpResponseMessage trsxsresponse = await this.GetUrl(this.Info.TrsxDownloadURL);
            //string what = await trsxsresponse.Content.ReadAsStringAsync();
            if (trsxsresponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _ = await trsxsresponse.Content.ReadAsStringAsync();
                MessageBox.Show("Problem with download", "Problem with download", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                this.LoadTranscription(WPFTranscription.Deserialize(await trsxsresponse.Content.ReadAsStreamAsync()));
            }
            catch
            {
                MessageBox.Show("document is in wrong format", "document is in wrong format", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Trans.MediaURI))
                try { this.Trans.MediaURI = this.Trans.Meta.Element("stream").Element("url").Value; }
                catch { };

            this.Trans.DocumentID = this.Info.DocumentId;
            await this.UpdateTranscriptionSpeakers();
        }
    }
}
