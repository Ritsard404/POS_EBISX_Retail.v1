using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

public static class NetworkHelper
{
    // reuse one HttpClient for the app’s lifetime
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };

    /// Returns true if we have any network AND can reach the Internet.
    public static async Task<bool> IsOnlineAsync()
    {
        // quick check for any up network interface
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        try
        {
            // this returns HTTP 204 No Content almost instantly
            using var resp = await _http.GetAsync("https://clients3.google.com/generate_204");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
