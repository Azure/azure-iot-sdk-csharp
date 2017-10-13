namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.Amqp;

    class IoTHubClientDiagnostic
    {
        const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const string DiagnosticCreationTimeUtcKey = "creationtimeutc";

        internal const string MqttDiagIdKey = "$.diagid";
        internal const string MqttDiagCorrelationContextKey = "$.diagctx";

        const string AmqpDiagIdKey = "Diagnostic-Id";
        const string AmqpDiagCorrelationContextKey = "Correlation-Context";

        internal static bool AddDiagnosticInfoIfNecessary(Message message, int diagnosticSamplingPercentage, ref int currentMessageCount)
        {
            bool result = false;

            if (ShouldAddDiagnosticInfo(diagnosticSamplingPercentage, ref currentMessageCount))
            {
                string creationTimeUtc = string.Format(CultureInfo.InvariantCulture, "{0}={1}", DiagnosticCreationTimeUtcKey, CurrentUtcTimeToSecond().ToString("0.000", CultureInfo.InvariantCulture));
                message.SystemProperties[MessageSystemPropertyNames.DiagId] = GenerateEightRandomCharacters();
                message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext] = creationTimeUtc;
                result = true;
            }

            return result;
        }

        internal static void CopyDiagnosticPropertiesToAmqpAnnotations(Message data, AmqpMessage amqpMessage)
        {
            if (HasDiagnosticProperties(data))
            {
                amqpMessage.MessageAnnotations.Map[AmqpDiagIdKey] = data.SystemProperties[MessageSystemPropertyNames.DiagId];
                amqpMessage.MessageAnnotations.Map[AmqpDiagCorrelationContextKey] = data.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext];
            }
        }

        internal static bool HasDiagnosticProperties(Message message)
        {
            return message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId) && message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext);
        }

        static string GenerateEightRandomCharacters()
        {
            var stringChars = new char[8];
            var random = new Random();
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }
            return new string(stringChars);
        }

        static bool ShouldAddDiagnosticInfo(int diagnosticSamplingPercentage, ref int currentMessageCount)
        {
            bool result = false;

            if (diagnosticSamplingPercentage > 0)
            {
                currentMessageCount = currentMessageCount == 100 ? 1 : currentMessageCount + 1;
                result = Math.Floor((currentMessageCount - 2) * diagnosticSamplingPercentage / 100.0) < Math.Floor((currentMessageCount - 1) * diagnosticSamplingPercentage / 100.0);
            }

            return result;
        }

        static double CurrentUtcTimeToSecond()
        {
            var dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan span = DateTime.UtcNow - dt1970;
            return span.TotalSeconds;
        }
    }
}
