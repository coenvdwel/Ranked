using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net;

namespace Ranked.Utility
{
  public class Slack
  {
    public static void SendMessage(string text, string channel, string icon, string username)
    {
      try
      {
        using (var client = new WebClient())
        {
          var uri = new Uri("https://hooks.slack.com/services/abcdefghijklmnopqrstuvwxyz");

          client.UploadValues(uri, "POST", new NameValueCollection
          {
            {
              "payload", JsonConvert.SerializeObject(new Payload
              {
                Channel = channel,
                Text = text,
                Icon = icon,
                Username = username
              })
            }
          });
        }
      }
      catch
      {
        // swallow
      }
    }

    private class Payload
    {
      // ReSharper disable UnusedAutoPropertyAccessor.Local
      // ReSharper disable UnusedMember.Local

      [JsonProperty("channel")]
      public string Channel { get; set; }

      [JsonProperty("text")]
      public string Text { get; set; }

      [JsonProperty("icon_emoji")]
      public string Icon { get; set; }

      [JsonProperty("username")]
      public string Username { get; set; }

      [JsonProperty("link_names")]
      public string LinkNames { get; set; } = "1";
    }
  }
}