//=======================================================================================
// Copyright © daenet GmbH Frankfurt am Main
//
// LICENSED UNDER THE APACHE LICENSE, VERSION 2.0 (THE "LICENSE"); YOU MAY NOT USE THESE
// FILES EXCEPT IN COMPLIANCE WITH THE LICENSE. YOU MAY OBTAIN A COPY OF THE LICENSE AT
// http://www.apache.org/licenses/LICENSE-2.0
// UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING, SOFTWARE DISTRIBUTED UNDER THE
// LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED. SEE THE LICENSE FOR THE SPECIFIC LANGUAGE GOVERNING
// PERMISSIONS AND LIMITATIONS UNDER THE LICENSE.
//=======================================================================================
using IotHubCommander.IotHubExport;
using Microsoft.Framework.Configuration.CommandLine;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotHubCommander
{
    class Program
    {
        /// <summary>
        /// Main Function 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine();
            Helper.WriteLine($"Welcome to IotHub Commander Tool [Version 1.0]{Environment.NewLine}(c) 2016 daenet GmbH, Frankfurt am Main, Germany.", ConsoleColor.Cyan);

            bool isHelp = Helper.isHelpCall(args);
            //Set up Environment variable
            Helper.CreateEnvironmentVariable();
            //
            //Check wether help is needed or not
            if (isHelp)
            {
                Helper.getHelp();
            }
            else
            {
                CommandLineConfigurationProvider cmdConfig = new CommandLineConfigurationProvider(args);
                try
                {
                    cmdConfig.Load();
                    string command;

                    if (cmdConfig.TryGet("migrateiothub", out command))
                    {
                        var job = new IotHubExportImport(cmdConfig);
                        job.Run();
                    }
                    else if (cmdConfig.TryGet("connectTo", out command)
                        || cmdConfig.TryGet("send", out command)
                        || cmdConfig.TryGet("listen", out command))
                    {
                        // Initialize the proper module, depending on the retrreived argument.
                        switch (command.ToLower())
                        {

                            case "eventhub":
                                EventListener(cmdConfig, null);
                                break;

                            case "iothub":
                                EventListener(cmdConfig, "messages/events");
                                break;
                            case "event":
                                D2CSend(cmdConfig);
                                break;
                            case "device":
                                C2DListen(cmdConfig);
                                break;
                            case "cloud":
                                C2DSend(cmdConfig);
                                break;
                            case "feedback":
                                GetFeedback(cmdConfig);
                                break;
                            default:
                                throw new Exception("Command not found. In order to see more details write \"--help\"");
                        }
                    }
                    else
                    {
                        throw new Exception("Command not found. In order to see more details write \"--help\"");
                    }
                }
                catch (Exception ex)
                {
                    // Print Exeption 
                    Helper.WriteLine(ex.ToString(), ConsoleColor.Magenta);
                }
            }
            Console.ReadKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdConfig"></param>
        private static void GetFeedback(CommandLineConfigurationProvider cmdConfig)
        {
            //--listen=Feedback --connStr="" action=""
            string connStr = cmdConfig.GetArgument("connStr");
            IHubModule feedbackReceiver = new FeedbackReceiver(connStr: connStr);
            feedbackReceiver.Execute().Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdConfig"></param>
        private static void C2DSend(CommandLineConfigurationProvider cmdConfig)
        {
            //--send=cloud --connstr="" enentfile="" --tempFile=""
            string connStr = cmdConfig.GetArgument("connStr");
            string eventFile = cmdConfig.GetArgument("eventFile");
            string templateFile = cmdConfig.GetArgument("tempFile");
            string deviceId = cmdConfig.GetArgument("deviceId");

            IHubModule devEmu = new Cloud2DeviceSender(connStr, deviceId, eventFile, templateFile);
            devEmu.Execute().Wait();
        }



        /// <summary>
        /// Send event Device to Cloud
        /// </summary>
        /// <param name="cmdConfig">CommandLineConfigurationProvider</param>
        private static void D2CSend(CommandLineConfigurationProvider cmdConfig)
        {
            //-send =event -connStr=connection_string -cmdDelay 5 -eventFile c:\temp\eventdata.csv -tempFile c:\jsontemplate.txt
            string connStr = cmdConfig.GetArgument("connStr");
            string cmdDelay = cmdConfig.GetArgument("cmdDelay");
            string eventFile = cmdConfig.GetArgument("eventFile");
            string templateFile = cmdConfig.GetArgument("tempFile");
            int commandDelayInSec = int.Parse(cmdDelay);
            IHubModule devEmu = new Device2CloudSender(connStr, commandDelayInSec, eventFile, templateFile);
            devEmu.Execute().Wait();
        }


        /// <summary>
        /// Event Listener Cloud to Device
        /// </summary>
        /// <param name="cmdConfig">CommandLineConfigurationProvider</param>
        private static void C2DListen(CommandLineConfigurationProvider cmdConfig)
        {
            string connStr = cmdConfig.GetArgument("connStr");
            string action = cmdConfig.GetArgument("action", false);
            CommandAction commandAction;
            bool isPressed = Enum.TryParse<CommandAction>(action, true, out commandAction);

            IHubModule devListener = new Cloud2DeviceListener(connStr, commandAction);
            devListener.Execute().Wait();

        }

        /// <summary>
        /// Event Listener for IotHub or EventHub
        /// </summary>
        /// <param name="cmdConfig">CommandLineConfigurationProvider</param>
        /// <param name="path">Path for Event Hub</param>
        private static void EventListener(CommandLineConfigurationProvider cmdConfig, string path = null)
        {
            string connStr = cmdConfig.GetArgument("connStr");
            string startTime = cmdConfig.GetArgument("startTime");
            string consumerGroup = cmdConfig.GetArgument("consumerGroup", false);
            DateTime time = Helper.getTime(startTime);
            if (consumerGroup != null)
            {
                IHubModule module = new TeleMetryListener(connStr, path, time, consumerGroup);
                var t = module.Execute();

                t.Wait(Timeout.Infinite);
            }
            else
            {
                IHubModule module = new TeleMetryListener(connStr, path, time);
                var t = module.Execute();

                t.Wait();
            }
        }
    }
}

