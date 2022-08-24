using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace App.Core.Editor
{
    public class ShellStartInfo
    {
        public string FileName { set; get; }
        public string Arguments { set; get; }
        public string WorkingDirectory { set; get; }

        public bool CreateWindow { set; get; }
        public bool RedirectOutput { set; get; }

        public ShellStartInfo()
        {
            FileName = string.Empty;
            Arguments = string.Empty;
            WorkingDirectory = string.Empty;
            CreateWindow = true;
            RedirectOutput = false;
        }
    }

    public class ShellReturnInfo
    {
        private readonly ShellStartInfo m_StartInfo;
        private readonly int m_ExitCode;
        private readonly string m_StandardOut;
        private readonly string m_StandardErr;

        public ShellStartInfo GetStartInfo()
        {
            return m_StartInfo;
        }

        public int GetExitCode()
        {
            return m_ExitCode;
        }

        public string GetStandardOut()
        {
            return m_StandardOut;
        }

        public string GetStandardErr()
        {
            return m_StandardErr;
        }

        public ShellReturnInfo(ShellStartInfo startInfo, int exitCode, string standardOut, string standardErr)
        {
            m_StartInfo = startInfo;
            m_ExitCode = exitCode;
            m_StandardOut = standardOut;
            m_StandardErr = standardErr;
        }
    }

    public static class Shell
    {
        public static void RunProcess(string fileName, string arguments, string workingDirectory)
        {
            RunProcess(new ShellStartInfo()
                {FileName = fileName, Arguments = "/c " + arguments, WorkingDirectory = workingDirectory});
        }

        public static void RunProcess(ShellStartInfo startInfo)
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.Arguments = startInfo.Arguments;
            processStartInfo.FileName = startInfo.FileName;
            processStartInfo.WorkingDirectory = startInfo.WorkingDirectory;
            processStartInfo.UseShellExecute = true;
            processStartInfo.CreateNoWindow = !startInfo.CreateWindow;
            processStartInfo.RedirectStandardOutput = startInfo.RedirectOutput;
            processStartInfo.RedirectStandardError = startInfo.RedirectOutput;
            processStartInfo.WindowStyle = ProcessWindowStyle.Normal;

            Debug.LogWarning("Args : " + processStartInfo.Arguments);

            Process process = Process.Start(processStartInfo);
            if (processStartInfo.RedirectStandardOutput && process != null)
            {
                var output = new StringBuilder();
                process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                    }
                });

                var error = new StringBuilder();
                process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                    }
                });
            }

            // if (process != null)
            // {
            //     process.WaitForExit();
            // }
        }
    }
}