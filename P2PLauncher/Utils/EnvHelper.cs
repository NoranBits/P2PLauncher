using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net;
using System.Security.Principal;

namespace P2PLauncher.Utils
{
    public static class EnvHelper
    {
        /// <summary>
        /// Get Program Files path (x64)
        /// Variable was introduced in Windows 7 (So it won't work in releases before W7)
        /// </summary>
        /// <returns>Path of Program Files</returns>
        public static string GetProgramFilesPath()
        {
            return Environment.ExpandEnvironmentVariables("%ProgramW6432%");
        }

        /// <summary>
        /// Get Program Files path (x86)
        /// Variable was introduced in Windows 7 (So it won't work in releases before W7)
        /// </summary>
        /// <returns>Path of Program Files x86</returns>
        public static string GetProgramFilesX86Path()
        {
            return Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
        }

        public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
        {
            ArgumentNullException.ThrowIfNull(memberExpression);
            if (memberExpression.Body is not MemberExpression expressionBody)
            {
                throw new ArgumentException("Expression body must be a MemberExpression", nameof(memberExpression));
            }
            return expressionBody.Member.Name;
        }

        public static bool Is64Bit()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
        }

        public static string Base64Encode(string plainText)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            ArgumentNullException.ThrowIfNull(base64EncodedData);
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static string GetPublicAddress()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string ip = http.GetStringAsync(new Uri("https://api.ipify.org")).GetAwaiter().GetResult();
                return ip.Trim();
            }
            catch (HttpRequestException ex)
            {
                ExceptionHelper.ShowMessageBox(ex);
                return "Unknown";
            }
            catch (TaskCanceledException ex)
            {
                ExceptionHelper.ShowMessageBox(ex);
                return "Unknown";
            }
        }

        public static void OpenNotepadWithFile(string fileLocation)
        {
            Process.Start("notepad.exe", fileLocation);
        }

        public static bool FileExists(string fileLocation)
        {
            return File.Exists(fileLocation);
        }
    }
}
