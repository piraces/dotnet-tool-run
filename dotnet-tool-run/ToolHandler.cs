﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;

namespace dotnettoolrun
{
    public class ToolHandler
    {
        private const string TEMP_FOLDER_NAME = "dotnettoolrun";

        private readonly string[] _toolManifestCliProcessArgs = { "new", "tool-manifest", "--force" };

        private readonly CliCommandLineWrapper _dotnetManifestCommand;
        private readonly CliCommandLineWrapper _dotnetInstallCommand;

        private string _tempPath = Assembly.GetExecutingAssembly().Location;
        private string _toolName;
        private string? _toolArgs;

        public ToolHandler(string toolName, string? version = null, string? framework = null, string? toolArgs = null)
        {
            InitializeTempFolder();
            _toolName = toolName;
            _toolArgs = toolArgs;
            _dotnetManifestCommand = new CliCommandLineWrapper(_toolManifestCliProcessArgs, true);
            var installArguments = GetToolInstallCliProcessArgs(toolName, version, framework);
            _dotnetInstallCommand = new CliCommandLineWrapper(installArguments, true);
        }

        public async Task<int> StartTool()
        {
            await _dotnetManifestCommand.StartCliCommand();
            await _dotnetInstallCommand.StartCliCommand();

            return await StartToolProcess();
        }
        
        private void InitializeTempFolder()
        {
            try
            {
                _tempPath = Path.GetTempPath();
            }
            catch (SecurityException)
            {
                Console.WriteLine("[X] Could not obtain access to temp path...");
                Console.WriteLine("[i] Using current directory");
            }
            finally
            {
                _tempPath = Path.Combine(_tempPath, TEMP_FOLDER_NAME);
            }
            
        }

        private string[] GetToolInstallCliProcessArgs(string toolName, string? version, string? framework) {
            var args = new[]
            {
                "tool",
                "install",
                toolName,
                "--tool-path",
                _tempPath,
            };

            if (!string.IsNullOrWhiteSpace(version))
            {
                args = args.Concat(new[] {"--version", version}).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(framework))
            {
                args = args.Concat(new[] { "--framework", framework }).ToArray();
            }

            return args;
        }

        private async Task<int> StartToolProcess()
        {
            var toolProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_tempPath, _toolName), WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = _toolArgs ?? string.Empty
                }
            };
            toolProcess.Start();
            await toolProcess.WaitForExitAsync();
            return toolProcess.ExitCode;
        }
    }
}