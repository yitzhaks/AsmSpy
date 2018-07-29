﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using AsmSpy.Core;
using static System.FormattableString;

namespace AsmSpy.CommandLine
{
    public class BindingRedirectExport : IDependencyVisualizer
    {
        private readonly IDependencyAnalyzerResult _result;
        private readonly string _exportFileName;
        private readonly ILogger _logger;

        public BindingRedirectExport(IDependencyAnalyzerResult result, string exportFileName, ILogger logger)
        {
            _result = result;
            _exportFileName = exportFileName;
            _logger = logger;
        }

        public void Visualize()
        {
            try
            {
                var document = Generate(_result, skipSystem: false);
                using (var writer = XmlWriter.Create(_exportFileName, new XmlWriterSettings{Indent = true}))
                {
                    document.WriteTo(writer);
                }
                _logger.LogMessage(string.Format(CultureInfo.InvariantCulture, "Exported to file {0}", _exportFileName));
            }
            catch (UnauthorizedAccessException uae)
            {
                _logger.LogError(string.Format(CultureInfo.InvariantCulture, "Could not write file {0} due to error {1}", _exportFileName, uae.Message));
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _logger.LogError(string.Format(CultureInfo.InvariantCulture, "Could not write file {0} due to error {1}", _exportFileName, dnfe.Message));
            }
        }

        public static XmlDocument Generate(IDependencyAnalyzerResult result, bool skipSystem)
        {
            var document = new XmlDocument();
            document.LoadXml(@"
                  <runtime>
                    <assemblyBinding xmlns=""urn: schemas - microsoft - com:asm.v1"">
                    </assemblyBinding>
                </runtime>");
            var assemblyGroups = result.Assemblies.Values.GroupBy(x => x.AssemblyName);

            foreach (var assemblyGroup in assemblyGroups.OrderBy(i => i.Key.Name))
            {
                if (skipSystem && AssemblyInformationProvider.IsSystemAssembly(assemblyGroup.Key))
                {
                    continue;
                }

                var assemblyInfos = assemblyGroup.OrderBy(x => x.AssemblyName.ToString()).ToList();
                if (assemblyInfos.Count <= 1)
                {
                    if (assemblyInfos.Count == 1 && assemblyInfos[0].AssemblySource == AssemblySource.Local)
                    {
                        continue;
                    }

                    if (assemblyInfos.Count <= 0)
                    {
                        continue;
                    }
                }

                var sortedAssemblies = assemblyInfos.OrderByDescending(a => a.AssemblyName.Version).ToList();
                var highestAssemblyVersion = sortedAssemblies.Select(a => a.AssemblyName).First().Version;
                var lowestAssemblyVersion = sortedAssemblies.Select(a => a.AssemblyName).Last().Version;
                var assemblyToUse = sortedAssemblies.FirstOrDefault(a => a.AssemblySource != AssemblySource.NotFound)?.AssemblyName;
                if (assemblyToUse == null)
                {
                    continue;
                }

                var depedententAssembly = document.CreateElement("dependentAssembly");
                // <assemblyIdentity name="NHibernate" publicKeyToken="aa95f207798dfdb4" culture="neutral" />
                // <bindingRedirect oldVersion="0.0.0.0-2.1.2.4000" newVersion="2.1.2.4000" />
                var assemblyIdentity = document.CreateElement("assemblyIdentity");
                assemblyIdentity.SetAttribute("name", assemblyToUse.Name);
                var publicKeyToken = GetPublicKeyTokenFromAssembly(assemblyToUse);
                if (publicKeyToken != null)
                {
                    assemblyIdentity.SetAttribute("publicKeyToken", publicKeyToken);
                }
                var cultureName = assemblyToUse.CultureName;
                assemblyIdentity.SetAttribute("culture", string.IsNullOrEmpty(cultureName) ? "neutral" : cultureName);
                depedententAssembly.AppendChild(assemblyIdentity);
                var bindingRedirect = document.CreateElement("bindingRedirect");
                bindingRedirect.SetAttribute("oldVersion", Invariant($"{lowestAssemblyVersion}-{highestAssemblyVersion}"));
                bindingRedirect.SetAttribute("newVersion", assemblyToUse.Version.ToString());
                depedententAssembly.AppendChild(bindingRedirect);
                document.DocumentElement.FirstChild.AppendChild(depedententAssembly);
            }

            return document;
        }

        private static string GetPublicKeyTokenFromAssembly(AssemblyName assembly)
        {
            var bytes = assembly.GetPublicKeyToken();
            if (!bytes?.Any() ?? true)
                return null;

            return string.Join(string.Empty, bytes.Select(@byte => @byte.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
