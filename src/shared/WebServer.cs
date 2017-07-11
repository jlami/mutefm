using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MuteFm
{
    public class WebServer
    {
        private static Dictionary<string, byte[]> _fileContents = new Dictionary<string, byte[]>();
        private static Dictionary<string, DateTime> _lastAccessDateTime = new Dictionary<string,DateTime>();
        public static void StoreFile(string fileName, byte[] contents)
        {
            //            MuteApp.SmartVolManagerPackage.SoundEventLogger.LogMsg("Stored file: " + fileName);
            _fileContents[fileName] = contents;
        }

        public static void ClearFile(string filename)
        {
            byte[] contents;
            if (_fileContents.TryGetValue(filename, out contents))
            {
                _fileContents.Remove(filename);
                _lastAccessDateTime.Remove(filename);
            }
        }

        public static void ClearOldEntries(DateTime beforeTime)
        {
            List<String> keys = _fileContents.Keys.ToList();

            int count = keys.Count();
            for (int i = 0; i < count; i++)
            {
                string key = keys[i];
                DateTime lastAccessTime = _lastAccessDateTime[key];

                if (lastAccessTime < beforeTime)
                {
                    _fileContents.Remove(key);
                    _lastAccessDateTime.Remove(key);
                }
            }
        }

        public static byte[] RetrieveFile(string filePath)
        {
            // First try to load from cache
            //            MuteApp.SmartVolManagerPackage.SoundEventLogger.LogMsg("Retrieved file request: " + fileName);
            byte[] contents = null;
            if (!_fileContents.TryGetValue(filePath, out contents))
            {
                try
                {
                    string pathOnDisk = GetPathForLocalFile(filePath);
                    FileInfo fInfo = new FileInfo(pathOnDisk);
                    if (fInfo.Exists)
                    {
                        long numBytes = fInfo.Length;

                        System.IO.FileStream fStream = new FileStream(pathOnDisk, FileMode.Open, FileAccess.Read);

                        System.IO.BinaryReader br = new BinaryReader(fStream);

                        contents = br.ReadBytes((int)numBytes);

                        br.Close();

                        fStream.Close();
                        StoreFile(filePath, contents); // cache for later
                    }
                }
                catch (Exception ex)
                {
                    MuteFm.SmartVolManagerPackage.SoundEventLogger.LogException(ex);
                }
            }
            _lastAccessDateTime[filePath] = DateTime.Now;

            return contents;
        }

        #region // From http://www.west-wind.com/weblog/posts/2009/Feb/05/Html-and-Uri-String-Encoding-without-SystemWeb
        /// <summary>
        /// UrlEncodes a string without the requirement for System.Web
        /// </summary>
        /// <param name="String"></param>
        /// <returns></returns>
        // [Obsolete("Use System.Uri.EscapeDataString instead")]
        public static string UrlEncode(string text)
        {
            // Sytem.Uri provides reliable parsing
            return System.Uri.EscapeDataString(text);
        }

        /// <summary>
        /// UrlDecodes a string without requiring System.Web
        /// </summary>
        /// <param name="text">String to decode.</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string text)
        {
            // pre-process for + sign space formatting since System.Uri doesn't handle it
            // plus literals are encoded as %2b normally so this should be safe
            text = text.Replace("+", " ");
            return System.Uri.UnescapeDataString(text);
        }
        /// <summary>
        /// Retrieves a value by key from a UrlEncoded string.
        /// </summary>
        /// <param name="urlEncoded">UrlEncoded String</param>
        /// <param name="key">Key to retrieve value for</param>
        /// <returns>returns the value or "" if the key is not found or the value is blank</returns>
        public static string GetUrlEncodedKey(string urlEncoded, string key)
        {
            urlEncoded = "&" + urlEncoded + "&";

            int Index = urlEncoded.IndexOf("&" + key + "=", StringComparison.OrdinalIgnoreCase);
            if (Index < 0)
                return "";

            int lnStart = Index + 2 + key.Length;

            int Index2 = urlEncoded.IndexOf("&", lnStart);
            if (Index2 < 0)
                return "";

            return UrlDecode(urlEncoded.Substring(lnStart, Index2 - lnStart));
        }
        public static string GetBaseUrl(string urlEncoded)
        {
            string[] ss = urlEncoded.Split(new char[] { '&', '?' });
            if (ss.Length > 0)
                return ss[0];
            else
                return "";
        }

        #endregion
        public static string GetInternalPlayerHtml()
        {
            // TODO: also cache it.  not useful until file ends up static
            System.IO.StreamReader sr = new System.IO.StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\mixer\internalmixer.html");
            string html = sr.ReadToEnd();
            sr.Close();
            return html;
        }
        public static string GetExtensionPlayerHtml()
        {
            // TODO: also cache it.  not useful until file ends up static
            System.IO.StreamReader sr = new System.IO.StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\mixer\mixer.html");
                        
            string html = sr.ReadToEnd();
            sr.Close();
            return html;
        }
        /*
        public static string GetPlayerHtml()
        {            
            // for images: have an images folder and have it automatically read in files if url matches.  cache file contents, too.

            MuteApp.BackgroundMusic bgMusic;
            string str = "<html><head><title>MuteTunes</title><style type='text/css'>a:link { color:white; } a:visited { color:white; } body {  background-color:black; margin-top: 1px; font-size: 12px; margin-bottom: 1px;}</style></head>";
            str += "<body>";
            str += "<select id='music_combo' onchange='getComboVal(this)'  style='font-family: monospace; font-size: 6pt;'>";

            bgMusic = SmartVolManagerPackage.BgMusicManager.ActiveBgMusic;
            str += "   <option value='" + bgMusic.Id + "'>" + bgMusic.GetName() + "</option>";
            for (int i = 0; i < SmartVolManagerPackage.BgMusicManager.MuteTunesConfig.BgMusics.Length; i++)
            {
                bgMusic = SmartVolManagerPackage.BgMusicManager.MuteTunesConfig.BgMusics[i];
                if (bgMusic.Id == SmartVolManagerPackage.BgMusicManager.ActiveBgMusic.Id)
                    continue;
                str += "   <option value='" + bgMusic.Id + "'>" + bgMusic.GetName() + "</option>";
            }
            str += "</select>";
            str += "&nbsp;&nbsp;&nbsp;<a href='javascript:document.location = document.location;'>Refresh</a>&nbsp;&nbsp;";
            str += "<a href='Play'>" + "Play" + "</a>  <a href='Pause'>Pause</a>  <a href='Stop'>Stop</a> <a href='Mute'>Mute</a> <a href='Unmute'>Unmute</a> <a href='Show'>Show</a> <a href='Hide'>Hide</a> <a href='Settings'>Settings</a> <a href='Exit'>Exit</a>";

            str += "<script type='text/javascript'>";

            str += "function getComboVal(sel) { window.open('/ChangeMusic&id=' + sel.options[sel.selectedIndex].value); };";
            str += "</script>";
            str += "</body></html>";

            return str;
        }*/

        private static string GetMediaImageHtml(string operation)
        {
            return "<img src='" + operation + ".png" + "' height=32 width=32 title='" + operation + "'>";
        }
        private static string GetPathForLocalFile(string fileName)
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\mixer\" + fileName;
        }

        public static string GetFileAsBase64String(string path)
        {
            byte[] contents = RetrieveFile(path);
            if (contents == null)
                return "";
            else
                return Convert.ToBase64String(contents);
        }

        public static System.Drawing.Bitmap GetBitmapFromWebServer(string path)
        {
            System.Drawing.Bitmap bitmap = null;
            byte[] contents = RetrieveFile(path);
            if (contents == null)
                bitmap = null;
            else
            {
                System.Drawing.ImageConverter ic = new System.Drawing.ImageConverter();
                System.Drawing.Image img = (System.Drawing.Image)ic.ConvertFrom(contents);
                bitmap = (System.Drawing.Bitmap)img;
            }

            return bitmap;
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
