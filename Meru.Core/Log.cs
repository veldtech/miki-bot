using IA.FileHandling;
using System;
using System.IO;

namespace IA
{
    public class Log
    {
        private static ClientInformation client;
        private static FileWriter log;

        public static void InitializeLogging(ClientInformation c)
        {
            client = c;
        }

        /// <summary>
        /// Display a [msg] message.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void Message(string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
			if (client.CanLog(LogLevel.MESSAGE))
			{
				Console.WriteLine("[msg]: " + message);
			}
        }

        /// <summary>
        /// Display a [!!!] message.
        /// </summary>
        /// <param name="message"></param>
        public static void Notice(string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            if (client.CanLog(LogLevel.NOTICE))
            {
                Console.WriteLine("[!!!]: " + message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a error message.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void Error(string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            if (client.CanLog(LogLevel.ERROR))
            {
                Console.WriteLine("[err]: " + message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a error message.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void ErrorAt(string target, string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            if (client.CanLog(LogLevel.ERROR))
            {
                Console.WriteLine("[err@{0}]: {1}", target, message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a warning message.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void Warning(string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[wrn]: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a warning message.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void WarningAt(string tag, string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[wrn@" + tag + "]: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a message when something is done.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void Done(string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[yay]: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Display a message when something is done.
        /// </summary>
        /// <param name="message">information about the action</param>
        public static void DoneAt(string target, string message)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            if (client.CanLog(LogLevel.NOTICE))
            {
                Console.WriteLine("[yay@{0}]: {1}", target, message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Logs custom messages
        /// </summary>
        /// <param name="message">message that appears.</param>
        /// <param name="color">the color the message appears in</param>
        /// <param name="logLevel">the level the message will be filterd on</param>
        public static void Print(string message, ConsoleColor color = ConsoleColor.White, LogLevel logLevel = LogLevel.MESSAGE)
        {
            if (client == null)
            {
                return;
            }

            Console.ForegroundColor = color;
            if (client.CanLog(logLevel))
            {
                Console.WriteLine(message);
            }
        }
    }
}