using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
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
        uploadProgress ??= new Progress<IDataTransferLevel> { };
        downloadProgress ??= new Progress<IDataTransferLevel> { };

        HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(httpRequest.Method), httpRequest.Target);

        if ((httpRequest.Data is null && httpRequest.Method.ToLower().Equals("post")
             ? new MemoryStream(new byte[0])
             : httpRequest.Data) is Stream { } data)
        {
            message.Content = new StreamContent(data);
        }

        if (httpRequest.Headers != null)
        {
            foreach (KeyValuePair<string, string> header in httpRequest.Headers)
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

        // Avoid aggressive caching on Windows Phone 8.1.
        message.Headers.Add("Cache-Control", "no-cache");
        message.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

        uploadProgress.Report(new DataTransferLevel { Amount = 0 });

        HttpResponseMessage response = await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        uploadProgress.Report(new DataTransferLevel { Amount = 1 });

        Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);



        MemoryStream resultStream = new MemoryStream { };
        int bufferSize = 4096, bytesRead = 0;
        byte[] buffer = new byte[bufferSize];
        long totalLength = -1, readSoFar = 0;

        try
        {
            totalLength = responseStream.Length;
        }
        catch
        {
            Console.WriteLine("Unsupported length...");
        };


        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await resultStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            readSoFar += bytesRead;

            if (totalLength > -1)
            {
                downloadProgress.Report(new DataTransferLevel { Amount = (double) readSoFar / totalLength });
            }
        }

        responseStream.Dispose(); 
                                  
        if (totalLength == -1)
        {
            downloadProgress.Report(new DataTransferLevel { Amount = 1.0 });
        }

        byte[] resultAsArray = resultStream.ToArray();
        resultStream.Dispose();

        // Assume UTF-8 encoding.
        string resultString = Encoding.UTF8.GetString(resultAsArray, 0, resultAsArray.Length);
        
        return new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);
    }

}
