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
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotHubCommander
{
    /// <summary>
    /// Send Event Device to Cloud
    /// </summary>
    internal class Device2CloudSender : IHubModule
    {
        #region Member Variables

        private int m_CommandDelayInSec;
        private string m_ConnStr;
        private string m_EvetFile;
        private string m_TemplateFile;

        #endregion
        /// <summary>
        /// Send event device to cloud
        /// </summary>
        /// <param name="connStr"> Device Connection string</param>
        /// <param name="commandDelayInSec">Command delay time</param>
        public Device2CloudSender(string connStr, int commandDelayInSec)
        {
            this.m_ConnStr = connStr;
            this.m_CommandDelayInSec = commandDelayInSec;
        }

        /// <summary>
        /// Send event device to cloud
        /// </summary>
        /// <param name="connStr">Connection string for device</param>
        /// <param name="commandDelayInSec">Command delay time</param>
        /// <param name="evetFile">csv formated event file. Every value should be ';' separated</param>
        /// <param name="templateFile">Event template</param>
        public Device2CloudSender(string connStr, 
            int commandDelayInSec, 
            string evetFile, 
            string templateFile) : this(connStr, commandDelayInSec)
        {
            this.m_EvetFile = evetFile;
            this.m_TemplateFile = templateFile;
        }

        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns></returns>
        public async Task Execute()
        {
            await sendEvent();
        }

        /// <summary>
        /// Send Event to cloud
        /// </summary>
        /// <returns></returns>
        private async Task sendEvent()
        {
            bool isColor =true;
            StreamReader readerEventFile = null;
            StreamReader readerTempFile = null;
            try
            {
                readerEventFile = new StreamReader(File.OpenRead(m_EvetFile));
                while (!readerEventFile.EndOfStream)
                {
                    readerTempFile = new StreamReader(File.OpenRead(m_TemplateFile));
                    var template = readerTempFile.ReadToEnd();

                    int cnt = 1;
                    var tokens = readerEventFile.ReadLine().Split(';');
                    foreach (var token in tokens)
                    {
                        string tkn = $"@{cnt}";
                        template = template.Replace(tkn, token);
                        cnt++;
                    }
                                      
                    var json = JsonConvert.SerializeObject(template);
                    var message = new Message(Encoding.UTF8.GetBytes(json));
                    message.MessageId = Guid.NewGuid().ToString();
                    DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(m_ConnStr);

                    await deviceClient.SendEventAsync(message);
                    if(isColor)
                    {
                       Helper.WriteLine($"Event sending ... {Environment.NewLine}{template}{Environment.NewLine} Event has been sent.",ConsoleColor.Green);
                    }
                    else
                    {
                        Helper.WriteLine($"Event sending ... {Environment.NewLine}{template}{Environment.NewLine} Event has been sent.",ConsoleColor.DarkYellow);
                    }
                    isColor = !isColor;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
