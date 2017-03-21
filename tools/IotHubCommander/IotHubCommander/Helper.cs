using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHubCommander
{
    public class Helper
    {

        #region Public Methods
        /// <summary>
        /// Help text
        /// </summary>
        public static void getHelp()
        {
            //--send=event --connStr=öoiwerjökj --cmdDelay=5 --eventFile=TextData1.csv --tempFile=JsonTemplate.txt
            StringBuilder helpD2C = new StringBuilder();
            StringBuilder exampleD2C = new StringBuilder();
            helpD2C.AppendLine();
            helpD2C.AppendLine($"- Send Event Device to Cloud -");
            helpD2C.AppendLine($"------------------------------");
            helpD2C.AppendLine($"------------------------------");
            helpD2C.AppendLine();
            helpD2C.AppendLine($"   --send=<\"Event\" for sending event>");
            helpD2C.AppendLine($"   --connStr=<Connection string for sending event>");
            helpD2C.AppendLine($"   --cmdDelay=<Delay time to listen>");
            helpD2C.AppendLine($"   --eventFile=<csv formated file with \";\" separated value>");
            helpD2C.AppendLine($"   --tempFile=<txt formated file, format of event>");
            exampleD2C.AppendLine($"- EXAMPLES -");
            exampleD2C.AppendLine($"--send=event --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvH/5HmRTkD8bR/YbEIU9IM= --cmdDelay=5 --eventFile=TextData1.csv --tempFile=JsonTemplate.txt");
            Helper.WriteLine(helpD2C.ToString(), ConsoleColor.White);
            Helper.WriteLine(exampleD2C.ToString(), ConsoleColor.Green);

            ////--send=cloud --connstr="" enentfile="" --tempFile=""
            StringBuilder helpC2D = new StringBuilder();
            StringBuilder exampleC2D = new StringBuilder();
            helpC2D.AppendLine($"- Send Event Cloud to Device -");
            helpC2D.AppendLine($"------------------------------");
            helpC2D.AppendLine($"------------------------------");
            helpC2D.AppendLine();
            helpC2D.AppendLine($"   --send=<\"Cloud\" for sending event>");
            helpC2D.AppendLine($"   --connStr=<Connection string for sending event>");
            helpC2D.AppendLine($"   --deviceId=<Connection string for sending event>");
            helpC2D.AppendLine($"   --eventFile=<csv formated file with \";\" separated value>");
            helpC2D.AppendLine($"   --tempFile=<txt formated file, format of event>");
            exampleC2D.AppendLine($"- EXAMPLES -");
            exampleC2D.AppendLine($"--send=Cloud --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvH/5HmRTkD8bR/YbEIU9IM= --eventFile=TextData1.csv --tempFile=JsonTemplate.txt");
            Helper.WriteLine(helpC2D.ToString(), ConsoleColor.White);
            Helper.WriteLine(exampleC2D.ToString(), ConsoleColor.Green);

            //--listen=Device --connStr=connection_string --autoCommit=true/false
            StringBuilder helpListener = new StringBuilder();
            StringBuilder exampleListener = new StringBuilder();
            helpListener.AppendLine($"- Cloud to Device Listener -");
            helpListener.AppendLine($"----------------------------");
            helpListener.AppendLine($"----------------------------");
            helpListener.AppendLine();
            helpListener.AppendLine($"   --listen=<\"Device\" for listening event>");
            helpListener.AppendLine($"   --connStr=<Connection string for reading event>");
            helpListener.AppendLine($"   --action=<\"Abandon, Commit or None\" for abandon, Commit the message. None is default command and will ask you for abandon or commit.>");           
            exampleListener.AppendLine($"- EXAMPLES -");
            exampleListener.AppendLine($"--listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Abandon");
            exampleListener.AppendLine($"--listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Commit");
            Helper.WriteLine(helpListener.ToString(), ConsoleColor.White);
            Helper.WriteLine(exampleListener.ToString(), ConsoleColor.Green);

            //--listen=Feedback --connStr=""
            StringBuilder helpFeedback = new StringBuilder();
            StringBuilder exampleFeedBack = new StringBuilder();
            helpFeedback.AppendLine($"- Cloud to Device Feedback -");
            helpFeedback.AppendLine($"----------------------------");
            helpFeedback.AppendLine($"----------------------------");
            helpFeedback.AppendLine();
            helpFeedback.AppendLine($"   --listen=<\"Feedback\" for listening event>");
            helpFeedback.AppendLine($"   --action=<\"Abandon, Commit or None\" for abandon, Commit the message. None is default command and will ask you for abandon or commit.>");
            exampleFeedBack.AppendLine($"- EXAMPLES -");
            exampleFeedBack.AppendLine($"--listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM=");
            Helper.WriteLine(helpFeedback.ToString(), ConsoleColor.White);
            Helper.WriteLine(exampleFeedBack.ToString(), ConsoleColor.Green);

            //-connectTo=EventHub -connStr=oadölfj -startTime=-3h -consumerGroup=$Default
            StringBuilder helpIotHub = new StringBuilder();
            StringBuilder exampleIotHub = new StringBuilder();
            helpIotHub.AppendLine($"- Read events form IoTHub or EventHub. -");
            helpIotHub.AppendLine($"----------------------------------------");
            helpIotHub.AppendLine($"----------------------------------------");
            helpIotHub.AppendLine();
            helpIotHub.AppendLine($"   --connectTo=<\"EventHub or IotHub\" for connect with>");
            helpIotHub.AppendLine($"   --connStr=<Connection string for reading event>");
            helpIotHub.AppendLine($"   --startTime=<Starting for reading for event that could be hour, day and now, otherwise it will throw an error \"Wrong time format\">");
            helpIotHub.AppendLine($"   --consumerGroup=<Your consumer group name. It is not mandatory, it will take \"$Default\" consumer group name>");
            exampleIotHub.AppendLine($"- EXAMPLES -");
            exampleIotHub.AppendLine($"--connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc -startTime=-3h -consumerGroup=abc");
            exampleIotHub.AppendLine($"--connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc -startTime=-3d -consumerGroup=abc");
            exampleIotHub.AppendLine($"--connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc -startTime=-3s -consumerGroup=abc");
            exampleIotHub.AppendLine($"--connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc -startTime=now -consumerGroup=abc");
            Helper.WriteLine(helpIotHub.ToString(), ConsoleColor.White);
            Helper.WriteLine(exampleIotHub.ToString(), ConsoleColor.Green);

        }

        /// <summary>
        /// Formating the DateTime
        /// </summary>
        /// <param name="startTime">Time in string </param>
        /// <returns></returns>
        public static DateTime getTime(string startTime)
        {

            DateTime time;
            if (startTime.ToLower().Contains('h'))
            {
                int hours = int.Parse(startTime.Substring(0, 2));
                time = DateTime.UtcNow.AddHours(hours);

            }
            else if (startTime.ToLower().Contains('s'))
            {
                int second = int.Parse(startTime.Substring(0, 2));
                time = DateTime.UtcNow.AddSeconds(second);
            }
            else if (startTime.ToLower().Contains('d'))
            {
                int day = int.Parse(startTime.Substring(0, 2));
                time = DateTime.UtcNow.AddDays(day);
            }
            else if (startTime.ToLower().Contains("now"))
            {
                time = DateTime.Now;
            }
            else
            {
                throw new Exception("Wrong time format.");
            }

            return time;
        }

        /// <summary>
        /// Checks args is empty and help is required or not 
        /// </summary>
        /// <param name="args"> args from main function </param>
        /// <returns></returns>
        public static bool isHelpCall(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith("help", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("--help", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("-help", StringComparison.OrdinalIgnoreCase) || args[i].StartsWith("/help", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = "--help=true";
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Color line print
        /// </summary>
        /// <param name="message">Text for printing</param>
        /// <param name="lineColor">Text Color</param>
        /// <param name="isNewLine">Print in new line</param>
        public static void WriteLine(string message, ConsoleColor lineColor, bool isNewLine = true)
        {
            Console.ForegroundColor = lineColor;
            if (isNewLine)
            {
                Console.WriteLine(message);
                Console.WriteLine();
            }
            else
            {
                Console.Write(message);
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Create Environment Variable
        /// </summary>
        public static void CreateEnvironmentVariable()
        {
            if (Environment.GetEnvironmentVariable("Path") == null)
            {
                var path = Environment.CurrentDirectory;
                Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.User);
            }
            else
            {
                var path = Environment.CurrentDirectory;
                string oldValue = string.Format($"{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User)}");
                if (!oldValue.Contains(path))
                {
                    string value = string.Format($"{oldValue};{path}");
                    Environment.SetEnvironmentVariable("Path", value, EnvironmentVariableTarget.User);
                }
            }
        }
        #endregion
    }
}
