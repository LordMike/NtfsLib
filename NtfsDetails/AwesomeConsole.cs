using System;
using System.Threading;

namespace NtfsDetails
{
    public static class AwesomeConsole
    {
        private static object _lockObj = new object();

        public static void WriteLine()
        {
            lock (_lockObj)
            {
                Console.WriteLine();
            }
        }

        public static void WriteLine(object obj)
        {
            if (obj == null)
                return;

            WriteLine(obj.ToString(), ConsoleColor.Gray);
        }

        public static void WriteLine(object obj, ConsoleColor color)
        {
            if (obj == null)
                return;

            WriteLine(obj.ToString(), color);
        }

        public static void WriteLine(string format, params object[] arguments)
        {
            WriteLine(format, ConsoleColor.Gray, arguments);
        }

        public static void WriteLine(string format, ConsoleColor color, params object[] arguments)
        {
            lock (_lockObj)
            {
                if (color != ConsoleColor.Gray)
                    Console.ForegroundColor = color;

                if (arguments.Length == 0)
                    Console.WriteLine(format);
                else
                    Console.WriteLine(format, arguments);

                if (color != ConsoleColor.Gray)
                    Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public static void Write(object obj)
        {
            if (obj == null)
                return;

            Write(obj.ToString());
        }

        public static void Write(object obj, ConsoleColor color)
        {
            if (obj == null)
                return;

            Write(obj.ToString(), color);
        }

        public static void Write(string format, params object[] arguments)
        {
            Write(format, ConsoleColor.Gray, arguments);
        }

        public static void Write(string format, ConsoleColor color, params object[] arguments)
        {
            lock (_lockObj)
            {
                if (color != ConsoleColor.Gray)
                    Console.ForegroundColor = color;

                if (arguments.Length == 0)
                    Console.Write(format);
                else
                    Console.Write(format, arguments);

                if (color != ConsoleColor.Gray)
                    Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public static ConsoleWriter BeginSequentialWrite()
        {
            return new ConsoleWriter();
        }

        public class ConsoleWriter : IDisposable
        {
            private bool _hasExited;
            private object _writerLockObj = new object();

            internal ConsoleWriter()
            {
                lock (_writerLockObj)
                    Monitor.Enter(_lockObj);
            }

            public void Close()
            {
                lock (_writerLockObj)
                {
                    if (_hasExited)
                        return;

                    Monitor.Exit(_lockObj);
                    _hasExited = true;
                }
            }

            public void Dispose()
            {
                Close();
            }
        }
    }
}