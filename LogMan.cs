using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogMan
{
    public static class LogMan
    {
        private static bool _init;
        private static string _current;
        private static object _l = new object();
        /// <summary>
        /// Prepares the LogMan for usage.
        /// Call LogMan.Initialize() at the entry point of your program.
        /// </summary>
        public static void Initialize()
        {
            lock (_l)
            {
                if (!_init)
                {
                    _init = true;
                    _nextOutput(false);
                }
            }
        }
        /// <summary>
        /// Logs some information to the console, without interrupting input.
        /// </summary>
        /// <param name="data">The data to output.</param>
        public static void Info(object data)
        {
            // The data must be ToString'ed here
            // (avoids potential deadlock)
            string text = data?.ToString();
            lock (_l)
            {
                // reset cur to 0 to overwrite any command text
                Console.SetCursorPosition(0, Console.CursorTop);
                // output intended log data
                Console.Write(text);
                // output empty text to clear line, leaving space for a new line
                Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft - 1));
                // newline char
                Console.WriteLine();
                // re-output any command text
                Console.Write(_current);
                // reset cur to old position
                Console.SetCursorPosition(_current.Length - _endIndex, Console.CursorTop);
            }
        }
        // All next/sim methods are called from ReadLine
        // and have synchronization there!
        private static int _endIndex = 0;
        private static void _nextOutput(bool newLine = true)
        {
            _endIndex = 0;
            _current = "> ";
            if (newLine) Console.Write("\n" + _current);
            else Console.Write(_current);
        }
        private static void _simLeftArr()
        {
            int maxAllowed = _current.Length - 2;
            if (_endIndex + 1 > maxAllowed)
            {
                // This movement is past the
                // "> " prefix. Disallow it.
            }
            else
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                _endIndex++;
            }
        }
        private static void _simRightArr()
        {
            int minAllowed = 0;
            if (_endIndex - 1 < minAllowed)
            {
                // This movement is past the
                // end of the input. Disallow it.
            }
            else
            {
                Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                _endIndex--;
            }
        }
        private static void _simEndKey()
        {
            Console.SetCursorPosition(_current.Length, Console.CursorTop);
            _endIndex = 0;
        }
        private static void _simHomeKey()
        {
            Console.SetCursorPosition(2, Console.CursorTop);
            _endIndex = _current.Length - 2;
        }
        private static void _simBackspace()
        {
            int maxAllowed = _current.Length - 2;
            if (_endIndex < maxAllowed)
            {
                int ind = _current.Length - _endIndex - 1;
                _current = _current.Remove(ind, 1);
                Console.Write("\b \b");
                // Re-output remaining chars
                Console.Write(_current.Substring(ind));
                // Replace final char
                Console.Write(" \b");
                // Move cursor back to original location
                Console.SetCursorPosition(ind, Console.CursorTop);
            }
        }
        private static void _simDelete()
        {
            int minAllowed = 0;
            if (_endIndex > minAllowed)
            {
                int ind = _current.Length - _endIndex;
                _current = _current.Remove(ind, 1);
                // Re-output remaining chars
                Console.Write(_current.Substring(ind));
                // Replace final char
                Console.Write(" \b");
                // Move cursor back to original location
                Console.SetCursorPosition(ind, Console.CursorTop);
                // Adjust end index because it has moved closer to end
                _endIndex--;
            }
        }
        private static void _nothingKey()
        {
            // used for keys that might ordinarily do some magic
            // but here, don't.
        }
        private const int RETURN = 13;
        // ReadLine should be called from one location at a time.
        // Since this is a console-managing class, this is generally only
        // called in the main function for a program.
        /// <summary>
        /// Reads a line from the console.
        /// LogMan.ReadLine() blocks until a line is available.
        /// </summary>
        /// <returns>The line input.</returns>
        public static string ReadLine()
        {
            while (true)
            {
                // This work-around is required due to the
                // odd behavior of Console.CursorLeft/Console.CursorTop used in LogMan.Info
                // under Unix platforms (see https://github.com/dotnet/runtime/issues/23850)
                while (!Console.KeyAvailable)
                    Thread.Sleep(25);

                ConsoleKeyInfo ch = Console.ReadKey(true);
                if (ch.KeyChar != RETURN)
                {
                    lock (_l)
                    {
                        if (ch.Key == ConsoleKey.LeftArrow)
                            _simLeftArr();
                        else if (ch.Key == ConsoleKey.RightArrow)
                            _simRightArr();
                        else if (ch.Key == ConsoleKey.Delete)
                            _simDelete();
                        else if (ch.Key == ConsoleKey.Backspace)
                            _simBackspace();
                        else if (ch.Key == ConsoleKey.End)
                            _simEndKey();
                        else if (ch.Key == ConsoleKey.Home)
                            _simHomeKey();
                        else if (_isWackKey(ch))
                            _nothingKey();
                        else
                        {
                            Console.Write(ch.KeyChar);
                            int ind = _current.Length - _endIndex;
                            _current = _current.Insert(ind, $"{ch.KeyChar}");
                            // Re-output remaining chars
                            Console.Write(_current.Substring(ind + 1));
                            // Move cursor back to original location
                            Console.SetCursorPosition(ind + 1, Console.CursorTop);
                        }
                    }
                }
                else break;
            }

            lock (_l)
            {
                string ret = _current.Substring(2);
                _nextOutput();
                return ret;
            }
        }
        // Returns whether a key is super wack
        // and should not be handled OR displayed.
        #region
        private static bool _isWackKey(ConsoleKeyInfo keyInfo)
        {
            // First, deal with some oddball cases
            // where ConsoleKey gives stuff like "Oem2"
            if (keyInfo.KeyChar == '?' ||
                keyInfo.KeyChar == '/' ||
                keyInfo.KeyChar == ';' ||
                keyInfo.KeyChar == ':' ||
                keyInfo.KeyChar == ',' ||
                keyInfo.KeyChar == '.' ||
                keyInfo.KeyChar == '@')
            {
                return false;
            }

            ConsoleKey key = keyInfo.Key;
            return
                key == ConsoleKey.PageUp ||
                key == ConsoleKey.PageDown ||
                key == ConsoleKey.Escape ||
                key == ConsoleKey.Zoom ||
                key == ConsoleKey.Applications ||
                key == ConsoleKey.Attention ||
                key == ConsoleKey.BrowserBack ||
                key == ConsoleKey.BrowserFavorites ||
                key == ConsoleKey.BrowserForward ||
                key == ConsoleKey.BrowserHome ||
                key == ConsoleKey.BrowserRefresh ||
                key == ConsoleKey.BrowserSearch ||
                key == ConsoleKey.BrowserStop ||
                key == ConsoleKey.Clear ||
                key == ConsoleKey.CrSel ||
                key == ConsoleKey.EraseEndOfFile ||
                key == ConsoleKey.Execute ||
                key == ConsoleKey.ExSel ||
                key == ConsoleKey.F1 ||
                key == ConsoleKey.F2 ||
                key == ConsoleKey.F3 ||
                key == ConsoleKey.F4 ||
                key == ConsoleKey.F5 ||
                key == ConsoleKey.F6 ||
                key == ConsoleKey.F7 ||
                key == ConsoleKey.F8 ||
                key == ConsoleKey.F9 ||
                key == ConsoleKey.F10 ||
                key == ConsoleKey.F11 ||
                key == ConsoleKey.F12 ||
                key == ConsoleKey.F13 ||
                key == ConsoleKey.F14 ||
                key == ConsoleKey.F15 ||
                key == ConsoleKey.F16 ||
                key == ConsoleKey.F17 ||
                key == ConsoleKey.F18 ||
                key == ConsoleKey.F19 ||
                key == ConsoleKey.F20 ||
                key == ConsoleKey.F21 ||
                key == ConsoleKey.F22 ||
                key == ConsoleKey.F23 ||
                key == ConsoleKey.F24 ||
                key == ConsoleKey.Help ||
                key == ConsoleKey.Insert ||
                key == ConsoleKey.LaunchApp1 ||
                key == ConsoleKey.LaunchApp2 ||
                key == ConsoleKey.LaunchMail ||
                key == ConsoleKey.LaunchMediaSelect ||
                key == ConsoleKey.LeftWindows ||
                key == ConsoleKey.MediaNext ||
                key == ConsoleKey.MediaPlay ||
                key == ConsoleKey.MediaPrevious ||
                key == ConsoleKey.MediaStop ||
                key == ConsoleKey.NoName ||
                key == ConsoleKey.Oem1 ||
                key == ConsoleKey.Oem2 ||
                key == ConsoleKey.Oem3 ||
                key == ConsoleKey.Oem4 ||
                key == ConsoleKey.Oem5 ||
                key == ConsoleKey.Oem6 ||
                key == ConsoleKey.Oem7 ||
                key == ConsoleKey.Oem8 ||
                key == ConsoleKey.Oem102 ||
                key == ConsoleKey.OemClear ||
                key == ConsoleKey.Pa1 ||
                key == ConsoleKey.Packet ||
                key == ConsoleKey.Pause ||
                key == ConsoleKey.Play ||
                key == ConsoleKey.Print ||
                key == ConsoleKey.PrintScreen ||
                key == ConsoleKey.Process ||
                key == ConsoleKey.RightWindows ||
                key == ConsoleKey.Select ||
                key == ConsoleKey.Separator ||
                key == ConsoleKey.Sleep ||
                key == ConsoleKey.VolumeDown ||
                key == ConsoleKey.VolumeMute ||
                key == ConsoleKey.VolumeUp;
        }
        #endregion
    }
}
