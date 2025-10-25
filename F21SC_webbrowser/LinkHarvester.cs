using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using F21SC_webbrowser;

namespace F21SC_webbrowser
{
    public class LinkHarvester
    {
        ///<summary>
        ///Extracts first 5 ancor hrefs from te current page DOM
        ///</summary>
        public async Task<List<(string href, string text)>> GetFiveLinksAsync(CoreWebView2 webView)
        {
            var resultList = new List<(string href, string text)>();
            if (webView == null)
                return resultList;

            try
            {
                string jsCode = @"
            (function() {
                try {
                    // Wait for full document readiness
                    if (document.readyState !== 'complete') return JSON.stringify([]);

                    const anchors = Array.from(document.querySelectorAll('a[href]'));
                    const links = anchors.slice(0, 5).map(a => {
                        return { href: a.href, text: a.innerText || a.href };
                    });
                    return JSON.stringify(links);
                } catch(e) {
                    return JSON.stringify([]);
                }
            })();
        ";

                string result = await webView.ExecuteScriptAsync(jsCode);

                // Clean WebView2 result (it's returned as a JSON string literal)
                if (result.StartsWith("\"")) result = result.Trim('"').Replace("\\\"", "\"");

                var links = System.Text.Json.JsonSerializer.Deserialize<List<LinkItem>>(result);
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        resultList.Add((link.href, link.text));
                    }
                }
            }
            catch
            {
                // swallow exceptions for now
            }

            return resultList;
        }

        private class LinkItem
        {
            public string href { get; set; }
            public string text { get; set; }
        }
    }
}