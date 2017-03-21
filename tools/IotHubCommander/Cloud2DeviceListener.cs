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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHubCommander
{
    /// <summary>
    /// Event Listener Cloud to Device
    /// </summary>
    internal class Cloud2DeviceListener : IHubModule
    {
        #region Member Variables

        ConsoleColor m_FeedbackClr = ConsoleColor.Cyan; 

        private CommandAction Action;
        private string m_ConnStr;
        private DeviceClient m_DeviceClient;

        #endregion

        #region Public Methods 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connStr"></param>
        public Cloud2DeviceListener(string connStr)
        {
            this.m_DeviceClient = DeviceClient.CreateFromConnectionString(connStr);
            this.m_ConnStr = connStr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="action"></param>
        public Cloud2DeviceListener(string connStr, CommandAction action) : this(connStr)
        {
            this.Action = action;
        }

        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns></returns>
        public async Task Execute()
        {
           await starReceivingCommands();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Receive the event from cloud 
        /// User can auto Commit, Abandon or None of them, user will be asked 'a' for Abandon and 'c' for Commit
        /// </summary>
        /// <returns></returns>
        private async Task starReceivingCommands()
        {
            bool isColor = true;
            Helper.WriteLine("\nStarted Receiving cloud to device messages from service",ConsoleColor.White);

            while (true)
            {
                Message receivedMessage = await m_DeviceClient.ReceiveAsync();
                if (receivedMessage == null)
                {
                    Helper.WriteLine("Commend receiver timed out. Reconnected...",ConsoleColor.Gray);
                    continue;
                }

                var bytes = receivedMessage.GetBytes();

                if (isColor)
                {
                    Helper.WriteLine($"Message Receiving ...{Environment.NewLine}Message ID: {receivedMessage.MessageId}{Environment.NewLine}Message: {Encoding.UTF8.GetString(bytes)}", ConsoleColor.DarkMagenta);
                }
                else
                {
                    Helper.WriteLine($"Message Receiving ...{Environment.NewLine}Message ID: {receivedMessage.MessageId}{Environment.NewLine}Message: {Encoding.UTF8.GetString(bytes)}", ConsoleColor.Green);
                }
                // switch color
                isColor = !isColor;

                try
                {
                    switch (Action)
                    {
                        case CommandAction.Complete:
                            await m_DeviceClient.CompleteAsync(receivedMessage);

                            Console.ForegroundColor = m_FeedbackClr;
                            Console.WriteLine("Command completed successfully (AutoCommit) :)!");
                            Console.ResetColor();
                            break;
                        case CommandAction.Abandon:
                            await m_DeviceClient.AbandonAsync(receivedMessage);

                            Console.ForegroundColor = m_FeedbackClr;
                            Console.WriteLine("Command abandoned successfully :)!");
                            Console.ResetColor();
                            break;
                        case CommandAction.None:
                        default:
                            Console.WriteLine();
                            Console.ForegroundColor = m_FeedbackClr;
                            Console.WriteLine("Enter 'a' for Abandon or 'c' for Complete");
                            Console.ResetColor();
                            string whatTodo = Console.ReadLine();
                            if (whatTodo == "a")
                            {
                                await m_DeviceClient.AbandonAsync(receivedMessage);

                                Console.ForegroundColor = m_FeedbackClr;
                                Console.WriteLine("Command abandoned successfully :)!");
                                Console.ResetColor();
                            }
                            else if (whatTodo == "c")
                            {
                                await m_DeviceClient.CompleteAsync(receivedMessage);

                                Console.ForegroundColor = m_FeedbackClr;
                                Console.WriteLine("Command completed successfully :)!");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = m_FeedbackClr;
                                Console.WriteLine("Receiving of commands has been stopped!");
                                Console.ResetColor();
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }

    #endregion

    #region Enum

    /// <summary>
    /// Command Action for None, Commint and Abandon
    /// </summary>
    public enum CommandAction
    {
        None = 0,
        Complete = 1,
        Abandon = 2
    }

    #endregion
}
