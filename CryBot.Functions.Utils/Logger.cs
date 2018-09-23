﻿using Microsoft.Azure.WebJobs.Host;

namespace CryBot.Functions.Utils
{
    public static class Logger
    {
        private static TraceWriter _logger;

        public static void Init(TraceWriter logger)
        {
            _logger = logger;
        }

        public static void Log(string message)
        {
            _logger.Info(message);
        }
    }
}
