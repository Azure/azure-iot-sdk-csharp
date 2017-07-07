using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IotHubCommander
{
    public class FeedbackReceiver : IHubModule
    {
        #region Member Variables 

        private ServiceClient m_ServiceClient;

        #endregion

        #region Public Methods

        public FeedbackReceiver(string connStr)
        {
            this.m_ServiceClient = ServiceClient.CreateFromConnectionString(connStr);
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <returns></returns>
        public async Task Execute()
        {
            await receiveFeedbackAsync();
        }

        #endregion

        #region Peivate Methods 

        /// <summary>
        /// Receive feedback 
        /// </summary>
        /// <returns></returns>
        private async Task receiveFeedbackAsync()
        {
            var feedbackReceiver = m_ServiceClient.GetFeedbackReceiver();

            Helper.WriteLine("\nReceiving Cloud to Device feedback from service", ConsoleColor.White);
            while (true)
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync();
                if (feedbackBatch == null) continue;
                bool isColor = true;
                foreach (var item in feedbackBatch.Records)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.AppendLine("Getting Feedback ...");
                    msg.AppendLine("Device ID: "+item.DeviceId);
                    msg.AppendLine("Message ID: "+item.OriginalMessageId);
                    msg.AppendLine("Time: "+item.EnqueuedTimeUtc.ToString());
                    msg.AppendLine("Status: "+item.StatusCode.ToString());
                    if (isColor)
                    {
                        Helper.WriteLine(msg.ToString(), ConsoleColor.Yellow);
                    }
                    else
                    {
                        Helper.WriteLine(msg.ToString(), ConsoleColor.DarkYellow);
                    }
                    isColor = !isColor;
                }
            }
        }
        #endregion
    }
}
       

