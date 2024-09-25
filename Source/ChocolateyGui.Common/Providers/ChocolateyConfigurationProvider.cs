﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="ChocolateyConfigurationProvider.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using chocolatey.infrastructure.filesystem;

namespace ChocolateyGui.Common.Providers
{
    public class ChocolateyConfigurationProvider : IChocolateyConfigurationProvider
    {
        private readonly IFileSystem _fileSystem;

        public ChocolateyConfigurationProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;

            GetChocolateyInstallLocation();
            DetermineIfChocolateyExecutableIsBeingUsed();
        }

        public string ChocolateyInstall { get; private set; }

        public bool IsChocolateyExecutableBeingUsed { get; private set; }

        private void GetChocolateyInstallLocation()
        {
#if DEBUG
            ChocolateyInstall = _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
#else
            ChocolateyInstall = Environment.GetEnvironmentVariable("ChocolateyInstall");
            if (string.IsNullOrWhiteSpace(ChocolateyInstall))
            {
                var pathVar = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrWhiteSpace(pathVar))
                {
                    ChocolateyInstall =
                        pathVar.Split(';')
                            .SingleOrDefault(
                                path => path.IndexOf("Chocolatey", StringComparison.OrdinalIgnoreCase) > -1);
                }
            }

            if (string.IsNullOrWhiteSpace(ChocolateyInstall))
            {
                ChocolateyInstall = string.Empty;
            }
#endif
        }

        private void DetermineIfChocolateyExecutableIsBeingUsed()
        {
#if DEBUG
            IsChocolateyExecutableBeingUsed = true;
#else
            var exePath = _fileSystem.CombinePaths(ChocolateyInstall, "choco.exe");

            if (_fileSystem.FileExists(exePath))
            {
                IsChocolateyExecutableBeingUsed = true;
            }
#endif
        }
    }
}