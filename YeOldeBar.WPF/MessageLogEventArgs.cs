﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YeOldePub
{
    public class MessageLogEventArgs: EventArgs
    {
        public string LogMessage { get; set; }

        public MessageLogEventArgs(string _logMessage)
        {
            LogMessage = _logMessage;
        }
    }
}
