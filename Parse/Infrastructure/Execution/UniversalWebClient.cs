using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;
using BCLWebClient = System.Net.Http.HttpClient;

namespace Parse.Infrastructure.Execution;

/// <summary>
/// A universal implementation of <see cref="IWebClient"/>.
/// </summary>
public class UniversalWebClient : IWebClient
{
    static HashSet<string> ContentHeaders { get; } = new HashSet<string>
    {
        { "Allow" },
        { "Content-Disposition" },
        { "Content-Encoding" },
        { "Content-Language" },
        { "Content-Length" },
        { "Content-Location" },
        { "Content-MD5" },
        { "Content-Range" },
        { "Content-Type" },
        { "Expires" },
        { "Last-Modified" }
    };

    public UniversalWebClient() : this(new BCLWebClient { }) { }

    public UniversalWebClient(BCLWebClient client) => Client = client;

    BCLWebClient Client { get; set; }

    public async Task<Tuple<HttpStatusCode, string>> ExecuteAsync(
WebRequest httpRequest,
IProgress<IDataTransferLevel> uploadProgress,
IProgress<IDataTransferLevel> downloadProgress,
CancellationToken cancellationToken)
    {
        uploadProgress ??= new Progress<IDataTransferLevel>();
        downloadProgress ??= new Progress<IDataTransferLevel>();

        using var message = new HttpRequestMessage(new HttpMethod(httpRequest.Method), httpRequest.Target);

        Stream data = httpRequest.Data;
        if (data != null || httpRequest.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            message.Content = new StreamContent(data ?? new MemoryStream(new byte[0]));
        }


        // Add headers to the message
        if (httpRequest.Headers != null)
        {
            foreach (var header in httpRequest.Headers)
            {
                if (ContentHeaders.Contains(header.Key))
                {
                    message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                else
                {
                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        // Avoid aggressive caching
        message.Headers.Add("Cache-Control", "no-cache");
        message.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

        uploadProgress.Report(new DataTransferLevel { Amount = 0 });

        using var response = await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        uploadProgress.Report(new DataTransferLevel { Amount = 1 });

        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var resultStream = new MemoryStream();

        var buffer = new byte[4096];
        int bytesRead;
        long totalLength = response.Content.Headers.ContentLength ?? -1;
        long readSoFar = 0;

        // Read response stream and report progress
        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await resultStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            readSoFar += bytesRead;

            if (totalLength > 0)
            {
                downloadProgress.Report(new DataTransferLevel { Amount = 1.0 * readSoFar / totalLength });
            }
        }

        // Report final progress if total length was unknown
        if (totalLength == -1)
        {
            downloadProgress.Report(new DataTransferLevel { Amount = 1.0 });
        }

        // Convert response to string (assuming UTF-8 encoding)
        var resultAsArray = resultStream.ToArray();
        string responseContent = Encoding.UTF8.GetString(resultAsArray);

        return new Tuple<HttpStatusCode, string>(response.StatusCode, responseContent);
    }

}
