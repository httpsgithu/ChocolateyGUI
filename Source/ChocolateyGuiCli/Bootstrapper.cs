﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Bootstrapper.cs" company="Chocolatey">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using Autofac;
using chocolatey.infrastructure.filesystem;
using ChocolateyGui.Common;
using ChocolateyGui.Common.Startup;
using ChocolateyGui.Common.Utilities;
using Serilog;
using Serilog.Events;

namespace ChocolateyGuiCli
{
    public static class Bootstrapper
    {
        private static readonly IFileSystem _fileSystem = new DotNetFileSystem();

#pragma warning disable SA1202

        // Due to an unknown reason, we can not use chocolateys own get_current_assembly() function here,
        // as it will be returning the path to the choco.exe executable instead.
        public static readonly string ChocolateyGuiInstallLocation = _fileSystem.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty));
        public static readonly string ChocolateyInstallEnvironmentVariableName = "ChocolateyInstall";

#if FORCE_CHOCOLATEY_OFFICIAL_KEY
        // always look at the official location of the machine installation
        public static readonly string ChocolateyInstallLocation = Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) ?? _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        public static readonly string LicensedGuiAssemblyLocation = _fileSystem.CombinePaths(ChocolateyInstallLocation, "extensions", "chocolateygui", "chocolateygui.licensed.dll");
#elif DEBUG
        public static readonly string ChocolateyInstallLocation = _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        public static readonly string LicensedGuiAssemblyLocation = _fileSystem.CombinePaths(ChocolateyInstallLocation, "extensions", "chocolateygui", "chocolateygui.licensed.dll");
#else
        // Install locations is Chocolatey.dll or choco.exe - In Release mode
        // we might be testing on a server or in the local debugger. Either way,
        // start from the assembly location and if unfound, head to the machine
        // locations instead. This is a merge of official and Debug modes.
        private static Assembly _assemblyForLocation = Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly() : Assembly.GetExecutingAssembly();
        public static readonly string ChocolateyInstallLocation = _fileSystem.FileExists(_fileSystem.CombinePaths(_fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase), "chocolatey.dll")) ||
                                                                  _fileSystem.FileExists(_fileSystem.CombinePaths(_fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase), "choco.exe")) ?
                _fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase) :
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName)) ?
                    Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) :
                    @"C:\ProgramData\Chocolatey"
            ;

        // when being used as a reference, start by looking next to Chocolatey, then in a subfolder.
        public static readonly string LicensedGuiAssemblyLocation = _fileSystem.FileExists(_fileSystem.CombinePaths(ChocolateyInstallLocation, "chocolateygui.licensed.dll")) ? _fileSystem.CombinePaths(ChocolateyInstallLocation, "chocolateygui.licensed.dll") : _fileSystem.CombinePaths(ChocolateyInstallLocation, "extensions", "chocolateygui", "chocolateygui.licensed.dll");
#endif

        public static readonly string ChocolateyGuiCommonAssemblyLocation = _fileSystem.CombinePaths(ChocolateyGuiInstallLocation, "ChocolateyGui.Common.dll");
        public static readonly string ChocolateyGuiCommonWindowsAssemblyLocation = _fileSystem.CombinePaths(ChocolateyGuiInstallLocation, "ChocolateyGui.Common.Windows.dll");

        public static readonly string ChocolateyGuiCommonAssemblySimpleName = "ChocolateyGui.Common";
        public static readonly string ChocolateyGuiCommonWindowsAssemblySimpleName = "ChocolateyGui.Common.Windows";

        public static readonly string UnofficialChocolateyGuiPublicKey = "ffc115b9f4eb5c26";
        public static readonly string OfficialChocolateyGuiPublicKey = "dfd1909b30b79d8b";

        public static readonly string Name = "Chocolatey GUI";

        public static readonly string LicensedChocolateyGuiAssemblySimpleName = "chocolateygui.licensed";
#pragma warning restore SA1202

        internal static ILogger Logger { get; private set; }

        internal static IContainer Container { get; private set; }

        internal static string AppDataPath { get; } = LogSetup.GetAppDataPath(Name);

        internal static string LocalAppDataPath { get; } = LogSetup.GetLocalAppDataPath(Name);

        internal static string UserConfigurationDatabaseName { get; } = "UserDatabase";

        internal static string GlobalConfigurationDatabaseName { get; } = "GlobalDatabase";

        internal static void Configure()
        {
            var logPath = LogSetup.GetLogsFolderPath("Logs");

            LogSetup.Execute();

            var directPath = Path.Combine(logPath, "ChocolateyGuiCli.{Date}.log");

            var logConfig = new LoggerConfiguration()
                .WriteTo.Sink(new ColouredConsoleSink(), LogEventLevel.Information)
                .WriteTo.Async(config =>
                    config.RollingFile(directPath, retainedFileCountLimit: 10, fileSizeLimitBytes: 150 * 1000 * 1000))
                .SetDefaultLevel();

            Logger = Log.Logger = logConfig.CreateLogger();

#if FORCE_CHOCOLATEY_OFFICIAL_KEY
            var chocolateyGuiPublicKey = OfficialChocolateyGuiPublicKey;
#else
            var chocolateyGuiPublicKey = UnofficialChocolateyGuiPublicKey;
#endif

            Container = AutoFacConfiguration.RegisterAutoFac(LicensedChocolateyGuiAssemblySimpleName, LicensedGuiAssemblyLocation, chocolateyGuiPublicKey);
        }
    }
}