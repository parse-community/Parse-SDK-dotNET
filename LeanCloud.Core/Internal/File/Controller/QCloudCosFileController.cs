using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal
{
    internal class QCloudCosFileController : AVFileController
    {
        private object mutex = new object();

        FileState fileState;
        Stream data;
        string bucket;
        string token;
        string uploadUrl;
        bool done;
        private long sliceSize = (long)CommonSize.KB512;

        public QCloudCosFileController(IAVCommandRunner commandRunner) : base(commandRunner)
        {
        }

        public Task<FileState> SaveAsync(FileState state,
            Stream dataStream,
            string sessionToken,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken)
        {
            if (state.Url != null)
            {
                return Task<FileState>.FromResult(state);
            }
            fileState = state;
            data = dataStream;
            return AVFileController.GetFileToken(fileState, cancellationToken).OnSuccess(_ =>
            {
                var fileToken = _.Result.Item2;
                uploadUrl = fileToken["upload_url"].ToString();
                token = fileToken["token"].ToString();
                fileState.ObjectId = fileToken["objectId"].ToString();
                bucket = fileToken["bucket"].ToString();

                return FileSlice(cancellationToken).OnSuccess(t =>
                {
                    if (done) return Task<FileState>.FromResult(state);
                    var response = t.Result.Item2;
                    var resumeData = response["data"] as IDictionary<string, object>;
                    if (resumeData.ContainsKey("access_url")) return Task<FileState>.FromResult(state);
                    var sliceSession = resumeData["session"].ToString();
                    var sliceOffset = long.Parse(resumeData["offset"].ToString());
                    return UploadSlice(sliceSession, sliceOffset, dataStream, progress, cancellationToken);
                }).Unwrap();

            }).Unwrap();
        }

        Task<FileState> UploadSlice(
            string sessionId,
            long offset,
            Stream dataStream,
            IProgress<AVUploadProgressEventArgs> progress,
            CancellationToken cancellationToken)
        {

            long dataLength = dataStream.Length;
            if (progress != null)
            {
                lock (mutex)
                {
                    progress.Report(new AVUploadProgressEventArgs()
                    {
                        Progress = AVFileController.CalcProgress(offset, dataLength)
                    });
                }
            }

            if (offset == dataLength)
            {
                return Task.FromResult<FileState>(fileState);
            }

            var sliceFile = GetNextBinary(offset, dataStream);
            return ExcuteUpload(sessionId, offset, sliceFile, cancellationToken).OnSuccess(_ =>
            {
                offset += sliceFile.Length;
                if (offset == dataLength)
                {
                    done = true;
                    return Task.FromResult<FileState>(fileState);
                }
                var response = _.Result.Item2;
                var resumeData = response["data"] as IDictionary<string, object>;
                var sliceSession = resumeData["session"].ToString();
                return UploadSlice(sliceSession, offset, dataStream, progress, cancellationToken);
            }).Unwrap();
        }

        Task<Tuple<HttpStatusCode, IDictionary<string, object>>> ExcuteUpload(string sessionId, long offset, byte[] sliceFile, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>();
            body.Add("op", "upload_slice");
            body.Add("session", sessionId);
            body.Add("offset", offset.ToString());

            return PostToQCloud(body, sliceFile, cancellationToken);
        }

        Task<Tuple<HttpStatusCode, IDictionary<string, object>>> FileSlice(CancellationToken cancellationToken)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            var body = new Dictionary<string, object>();
            if (data.Length <= (long)CommonSize.KB512)
            {
                body.Add("op", "upload");
                body.Add("sha", HexStringFromBytes(sha1.ComputeHash(data)));
                var wholeFile = GetNextBinary(0, data);
                return PostToQCloud(body, wholeFile, cancellationToken).OnSuccess(_ =>
                {
                    if (_.Result.Item1 == HttpStatusCode.OK)
                    {
                        done = true;
                    }
                    return _.Result;
                });
            }
            else
            {
                body.Add("op", "upload_slice");
                body.Add("filesize", data.Length);
                body.Add("sha", HexStringFromBytes(sha1.ComputeHash(data)));
                body.Add("slice_size", (long)CommonSize.KB512);
            }

            return PostToQCloud(body, null, cancellationToken);
        }
        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        public static string SHA1HashStringForUTF8String(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }
        Task<Tuple<HttpStatusCode, IDictionary<string, object>>> PostToQCloud(
            Dictionary<string, object> body,
            byte[] sliceFile,
            CancellationToken cancellationToken)
        {
            IList<KeyValuePair<string, string>> sliceHeaders = new List<KeyValuePair<string, string>>();
            sliceHeaders.Add(new KeyValuePair<string, string>("Authorization", this.token));

            string contentType;
            long contentLength;

            var tempStream = HttpUploadFile(sliceFile, fileState.CloudName, out contentType, out contentLength, body);

            sliceHeaders.Add(new KeyValuePair<string, string>("Content-Type", contentType));

            var rtn = AVClient.RequestAsync(new Uri(this.uploadUrl), "POST", sliceHeaders, tempStream, null, cancellationToken).OnSuccess(_ =>
            {
                var dic = AVClient.ReponseResolve(_.Result, CancellationToken.None);

                return dic;
            });

            return rtn;
        }
        public static Stream HttpUploadFile(byte[] file, string fileName, out string contentType, out long contentLength, IDictionary<string, object> nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = StringToAscii("\r\n--" + boundary + "\r\n");
            contentType = "multipart/form-data; boundary=" + boundary;

            MemoryStream rs = new MemoryStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            if (file != null)
            {
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, "fileContent", fileName, "application/octet-stream");
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                rs.Write(file, 0, file.Length);
            }

            byte[] trailer = StringToAscii("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            contentLength = rs.Length;

            rs.Position = 0;
            var tempBuffer = new byte[rs.Length];
            rs.Read(tempBuffer, 0, tempBuffer.Length);

            return new MemoryStream(tempBuffer);
        }

        public static byte[] StringToAscii(string s)
        {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }

        byte[] GetNextBinary(long completed, Stream dataStream)
        {
            if (completed + sliceSize > dataStream.Length)
            {
                sliceSize = dataStream.Length - completed;
            }

            byte[] chunkBinary = new byte[sliceSize];
            dataStream.Seek(completed, SeekOrigin.Begin);
            dataStream.Read(chunkBinary, 0, (int)sliceSize);
            return chunkBinary;
        }
    }
}
