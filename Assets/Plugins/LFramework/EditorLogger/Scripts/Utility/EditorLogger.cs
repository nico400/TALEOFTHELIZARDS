using UnityEngine;

namespace LFramework.Utility
{
    /// <summary>
    /// Provides simplified logging utilities for use inside the Unity Editor.
    /// Automatically disabled in builds.
    /// </summary>
    public class EditorLogger
    {
        public static readonly EditorLogHandler LogHandler = new();
        public static readonly EditorErrorHandler ErrorHandler = new();
        public static readonly EditorWarningHandler WarningHandler = new();

        /// <summary>
        /// Logs a standard message using the specified log handler. Editor-only.
        /// </summary>
        public static void Log(string text, ILoggerHandler handler)
        {
#if UNITY_EDITOR
            handler.Log(text);
#endif
        }

        /// <summary>
        /// Logs a standard message. Editor-only.
        /// </summary>
        public static void Log(string text)
        {
            Log(text, LogHandler);
        }

        /// <summary>
        /// Logs a warning message. Editor-only.
        /// </summary>
        public static void LogWarning(string text)
        {
            Log(text, WarningHandler);
        }

        /// <summary>
        /// Logs an error message. Editor-only.
        /// </summary>
        public static void LogError(string text)
        {
            Log(text, ErrorHandler);
        }

        /// <summary>
        /// Logs a colored message using the specified log handler. Editor-only.
        /// </summary>
        public static void LogColored(string text, Color color, ILoggerHandler handler)
        {
#if UNITY_EDITOR
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            string coloredText = $"<color=#{hexColor}>{text}</color>";
            handler.Log(coloredText);
#endif
        }

        /// <summary>
        /// Logs a colored standard message. Editor-only.
        /// </summary>
        public static void Log(string text, Color color)
        {
            LogColored(text, color, LogHandler);
        }

        /// <summary>
        /// Logs a colored warning message. Editor-only.
        /// </summary>
        public static void LogWarning(string text, Color color)
        {
            LogColored(text, color, WarningHandler);
        }

        /// <summary>
        /// Logs a colored error message. Editor-only.
        /// </summary>
        public static void LogError(string text, Color color)
        {
            LogColored(text, color, ErrorHandler);
        }
    }

    /// <summary>
    /// Defines a handler for processing logged messages.
    /// </summary>
    public interface ILoggerHandler
    {
        /// <summary>
        /// Handles a log message.
        /// </summary>
        void Log(string msg);
    }

    /// <summary>
    /// Handles standard log messages for the Unity Editor Console.
    /// </summary>
    public class EditorLogHandler : ILoggerHandler
    {
        /// <summary>
        /// Logs a standard message to the Console. Editor-only.
        /// </summary>
        public void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#endif
        }
    }

    /// <summary>
    /// Handles warning messages for the Unity Editor Console.
    /// </summary>
    public class EditorWarningHandler : ILoggerHandler
    {
        /// <summary>
        /// Logs a warning message to the Console. Editor-only.
        /// </summary>
        public void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.LogWarning(msg);
#endif
        }
    }

    /// <summary>
    /// Handles error messages for the Unity Editor Console.
    /// </summary>
    public class EditorErrorHandler : ILoggerHandler
    {
        /// <summary>
        /// Logs an error message to the Console. Editor-only.
        /// </summary>
        public void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.LogError(msg);
#endif
        }
    }
}