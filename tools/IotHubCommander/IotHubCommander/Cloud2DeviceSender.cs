using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHubCommander
{
    public class Cloud2DeviceSender : IHubModule
    {
        #region Member Variables 

        private ServiceClient m_ServiceClient;
        private string m_DeviceId;
        private string m_EvetFile;
        private string m_TemplateFile;
        #endregion

        #region Public Methods

        public Cloud2DeviceSender(string connStr)
        {
            this.m_ServiceClient = ServiceClient.CreateFromConnectionString(connStr);
        }

        public Cloud2DeviceSender(string connStr, string deviceId, string eventFilePath, string tempFilePath):this(connStr)
        {
            this.m_DeviceId = deviceId;
            this.m_EvetFile = eventFilePath;
            this.m_TemplateFile = tempFilePath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Execute()
        {
           await sendEvent();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task sendEvent()
        {
            bool isColor = true;
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
                    message.Ack = DeliveryAcknowledgement.Full;

                    await m_ServiceClient.SendAsync(m_DeviceId,message);
                    //
                    //Different color
                    if (isColor)
                    {
                        Helper.WriteLine($"Event sending ...{Environment.NewLine}{template}{Environment.NewLine} Event has been sent.", ConsoleColor.White);
                    }
                    else
                    {
                        Helper.WriteLine($"Event sending ...{Environment.NewLine}{template}{Environment.NewLine} Event has been sent.", ConsoleColor.DarkGreen);
                    }
                    isColor = !isColor;

                    // Thread.Sleep(3000);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
