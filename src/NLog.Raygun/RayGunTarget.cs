using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net;
using NLog.Config;
using NLog.Targets;
using System.Collections;

namespace NLog.Raygun
{
    [Target("RayGun")]
    public class RayGunTarget : TargetWithLayout
    {
        [RequiredParameter]
        public string ApiKey { get; set; }

        public string CustomDataFieldNames { get; set; }

        public string Tags { get; set; }

        public string IgnoreFormFieldNames { get; set; }

        public string IgnoreCookieNames { get; set; }

        public string IgnoreServerVariableNames { get; set; }

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
                IDictionary customData = ExtractCustomDataFromException(exception);

                RaygunClient raygunClient = CreateRaygunClient();
                SendMessage(raygunClient, exception, tags, customData);
            }
            else
            {
                string logMessage = Layout.Render(logEvent);

                RaygunException exception = new RaygunException(logMessage, logEvent.Exception);
                RaygunClient client = CreateRaygunClient();

                List<string> tags = ExtractTagsFromException(exception);
                IDictionary customData = ExtractCustomDataFromException(exception);

                SendMessage(client, exception, tags, customData);
            }
        }

        private static bool IsException(LogEventInfo logEvent)
        {
            return logEvent.Parameters != null && logEvent.Parameters.Any() && logEvent.Parameters.FirstOrDefault() != null && logEvent.Parameters.First().GetType() == typeof(Exception);
        }

        private IDictionary ExtractCustomDataFromException(Exception exception)
        {
            var dictionary = new Dictionary<string, string>();

            foreach(var customDataField in SplitValues(CustomDataFieldNames))
            {
                if (exception.Data.Contains(customDataField))
                {
                    dictionary.Add(customDataField, exception.Data[customDataField]?.ToString());
                }
            }

            return dictionary;
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

        private void SendMessage(RaygunClient client, Exception exception, IList<string> exceptionTags, IDictionary customData)
        {
            if (!string.IsNullOrWhiteSpace(Tags))
            {
                var tags = Tags.Split(',');

                foreach (string tag in tags)
                {
                    exceptionTags.Add(tag);
                }
            }

            client.SendInBackground(exception, exceptionTags, customData);
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