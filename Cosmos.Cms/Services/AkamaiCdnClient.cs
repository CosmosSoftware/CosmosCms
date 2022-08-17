namespace Cosmos.Cms.Services
{
    //public class AkamaiCdnClient
    //{
    //    private readonly string _accessToken;
    //    private readonly string _akamaiApiHostUrl;
    //    private readonly string _clientSecret;
    //    private readonly string _clientToken;

    //    public AkamaiCdnClient(string clientToken, string accessToken, string clientSecret, string akamaiApiHostUrl)
    //    {
    //        _akamaiApiHostUrl = akamaiApiHostUrl;
    //        var error = string.Empty;
    //        if (string.IsNullOrEmpty(clientSecret))
    //            error = "clientToken is empty. ";
    //        if (string.IsNullOrEmpty(accessToken))
    //            error += "accessToken is empty. ";
    //        if (string.IsNullOrEmpty(clientSecret))
    //            error += "clientSecret is empty.";

    //        if (error == string.Empty)
    //        {
    //            _clientToken = clientToken;
    //            _accessToken = accessToken;
    //            _clientSecret = clientSecret;
    //            Guid.NewGuid();
    //        }
    //        else
    //        {
    //            throw new Exception(error);
    //        }
    //    }

    //    //public WebRequest CreateWebRequest(string endPoint, string method)
    //    //{
    //    //    var webRequest = WebRequest.Create(new Uri($"https://{_akamaiApiHostUrl}{endPoint}"));
    //    //    webRequest.Method = method;
    //    //    webRequest.ContentType = "application/json";
    //    //    ServicePointManager.Expect100Continue = false; var credentials = new ClientCredential(_clientToken, _accessToken, _clientSecret);

    //    //    var signer = new EdgeGridV1Signer(null, 100000);

    //    //    webRequest = signer.Sign(webRequest, credentials);
    //    //    return webRequest;
    //    //}

    //    private WebRequest CreateWebRequest(string endPoint, string method, object jsonObject)
    //    {
    //        var webRequest = WebRequest.Create(new Uri($"https://{_akamaiApiHostUrl}{endPoint}"));
    //        webRequest.Method = method;
    //        webRequest.ContentType = "application/json";
    //        ServicePointManager.Expect100Continue = false;

    //        var byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonObject));
    //        var uploadStream = new MemoryStream(byteArray);

    //        var signer = new EdgeGridV1Signer(null, 100000);

    //        webRequest = signer.Sign(webRequest, new ClientCredential(_clientToken, _accessToken, _clientSecret),
    //            uploadStream);

    //        if (uploadStream.CanSeek)
    //            webRequest.ContentLength = uploadStream.Length;
    //        else if (webRequest is HttpWebRequest)
    //            ((HttpWebRequest)webRequest).SendChunked = true;

    //        // avoid internal memory allocation before buffering the output
    //        if (webRequest is HttpWebRequest)
    //            ((HttpWebRequest)webRequest).AllowWriteStreamBuffering = false;

    //        using var requestStream = webRequest.GetRequestStream();
    //        using (uploadStream)
    //        {
    //            uploadStream.CopyTo(requestStream, 1024 * 1024);
    //        }

    //        return webRequest;
    //    }

    //    /// <summary>
    //    ///     Purges Akamai CDN
    //    /// </summary>
    //    /// <param name="purgeObject"></param>
    //    /// <param name="endpoint"></param>
    //    /// <returns></returns>
    //    public string PurgeProduction(AkamaiPurgeObjects purgeObject, string endpoint)
    //    {
    //        // http://asparticles.com/Articles/103/how-to-post-json-data-to-webapi-using-csharp
    //        var request = CreateWebRequest(endpoint, "POST", purgeObject);

    //        try
    //        {
    //            // Fails with 401 Signature does not match
    //            var httpResponse = (HttpWebResponse)request.GetResponse();
    //            using var reader = new StreamReader(httpResponse.GetResponseStream()!, Encoding.ASCII);
    //            var responseText = reader.ReadToEnd();
    //            return responseText;
    //        }
    //        catch (WebException e)
    //        {
    //            using var response = e.Response;
    //            var httpResponse = (HttpWebResponse)response;
    //            Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
    //            using var data = response.GetResponseStream();
    //            using var reader = new StreamReader(data!);
    //            var text = reader.ReadToEnd();
    //            throw new Exception(text);
    //        }
    //    }

    //    /// <summary>
    //    ///     Array of URLs to purge
    //    /// </summary>
    //    /// <param name="hostName"></param>
    //    /// <param name="urls">paths to purge</param>
    //    /// <returns>Purge result JSON</returns>
    //    public string PurgeProductionByUrls(string hostName, string[] paths)
    //    {
    //        var purgeObjects = new AkamaiPurgeObjects
    //        {
    //            Hostname = hostName,
    //            Objects = paths
    //        };

    //        return PurgeProduction(purgeObjects, PurgeEndPoints.UrlProductionEndpoint);
    //    }
    //}
}