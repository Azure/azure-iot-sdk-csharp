// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common
{
    internal static class ResourceHelper
    {
        /// <summary>
        ///  When formatting a message with args, truncate each arg to 1024 characters max
        /// </summary>
        /// <param name="message">The message to format</param>
        /// <param name="args">The arguments to format into the message</param>
        /// <returns>A potentially truncated message with args formatted in.</returns>
        internal static string TruncateFormattedArgs(
            string message,
            params object[] args)
        {
            if (args == null ||
                args.Length <= 0)
            {
                return message;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string text = args[i] as string;
                if (text != null && text.Length > 1024)
                {
                    args[i] = text.Substring(0, 1021) + "...";
                }
            }

            return string.Format(Resources.Culture, message, args);
        } 
    }
}
