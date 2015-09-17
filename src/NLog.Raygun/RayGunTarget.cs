using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net;
using NLog.Config;
using NLog.Targets;

namespace NLog.Raygun
{
  [Target("RayGun")]
  public class RayGunTarget : TargetWithLayout
  {
    [RequiredParameter]
    public string ApiKey { get; set; }

    [RequiredParameter]
    public string Tags { get; set; }

    [RequiredParameter]
    public string IgnoreFormFieldNames { get; set; }

    [RequiredParameter]
    public string IgnoreCookieNames { get; set; }

    [RequiredParameter]
    public string IgnoreServerVariableNames { get; set; }

    [RequiredParameter]
    public string IgnoreHeaderNames { get; set; }

    [RequiredParameter]
    public bool UseIdentityNameAsUserId { get; set; }

    protected override void Write(LogEventInfo logEvent)
    {
      // If we have a real exception, we can log it as is, otherwise we can take the NLog message and use that.
      if (IsException(logEvent))
      {
        Exception exception = (Exception)logEvent.Parameters.First();

        List<string> tags = ExtractTagsFromException(exception);

        RaygunClient raygunClient = CreateRaygunClient();
        SendMessage(raygunClient, exception, tags);
      }
      else
      {
        string logMessage = Layout.Render(logEvent);

        RaygunException exception = new RaygunException(logMessage, logEvent.Exception);
        RaygunClient client = CreateRaygunClient();

        SendMessage(client, exception, new List<string>());
      }
    }

    private static bool IsException(LogEventInfo logEvent)
    {
      return logEvent.Parameters.Any() && logEvent.Parameters.FirstOrDefault() != null && logEvent.Parameters.First().GetType() == typeof(Exception);
    }

    private static List<string> ExtractTagsFromException(Exception exception)
    {
      // Try and get tags off the exception data, if they exist
      List<string> tags = new List<string>();
      if (exception.Data["Tags"] != null)
      {
        if (exception.Data["Tags"].GetType() == typeof(List<string>))
        {
          tags.AddRange((List<string>)exception.Data["Tags"]);
        }

        if (exception.Data["Tags"].GetType() == typeof(string[]))
        {
          tags.AddRange(((string[])exception.Data["Tags"]).ToList());
        }
      }
      return tags;
    }

    private RaygunClient CreateRaygunClient()
    {
      var client = new RaygunClient(ApiKey);

      client.IgnoreFormFieldNames(SplitValues(IgnoreFormFieldNames));
      client.IgnoreCookieNames(SplitValues(IgnoreCookieNames));
      client.IgnoreHeaderNames(SplitValues(IgnoreHeaderNames));
      client.IgnoreServerVariableNames(SplitValues(IgnoreServerVariableNames));

      return client;
    }

    private void SendMessage(RaygunClient client, Exception exception, IList<string> exceptionTags)
    {
      if (!string.IsNullOrWhiteSpace(Tags))
      {
        var tags = Tags.Split(',');

        foreach (string tag in tags)
        {
          exceptionTags.Add(tag);
        }
      }

      client.SendInBackground(exception, exceptionTags);
    }

    private string[] SplitValues(string input)
    {
      if (!string.IsNullOrWhiteSpace(input))
      {
        return input.Split(',');
      }

      return new[] { string.Empty };
    }
  }
}