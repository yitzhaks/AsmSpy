﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AsmSpy.Core;

namespace AsmSpy.CommandLine
{
    public class ConsoleVisualizer : IDependencyVisualizer
    {
        private const ConsoleColor AssemblyNotFoundColor = ConsoleColor.Red;
        private const ConsoleColor AssemblyLocalColor = ConsoleColor.Green;
        private const ConsoleColor AssemblyGlobalAssemblyCacheColor = ConsoleColor.Yellow;
        private const ConsoleColor AssemblyUnknownColor = ConsoleColor.Magenta;

        #region Fields

        private readonly IDependencyAnalyzerResult _analyzerResult;

        #endregion

        #region Properties

        public bool OnlyConflicts { get; set; }

        public bool SkipSystem { get; set; }

        public string ReferencedStartsWith { get; set; }

        #endregion

        #region Constructor

        public ConsoleVisualizer(IDependencyAnalyzerResult result)
        {
            _analyzerResult = result;
        }

        #endregion

        #region Visualize Support

        public virtual void Visualize()
        {
            if (_analyzerResult.AnalyzedFiles.Count <= 0)
            {
                Console.WriteLine(AsmSpy_CommandLine.No_assemblies_files_found_in_directory);
                return;
            }

            if (OnlyConflicts)
            {
                Console.WriteLine(AsmSpy_CommandLine.Detailing_only_conflicting_assembly_references);
            }

            var assemblyGroups = _analyzerResult.Assemblies.Values.GroupBy(x => x.AssemblyName);

            foreach (var assemblyGroup in assemblyGroups.OrderBy(i => i.Key.Name))
            {
                if (SkipSystem && AssemblyInformationProvider.IsSystemAssembly(assemblyGroup.Key))
                {
                    continue;
                }

                var assemblyInfos = assemblyGroup.OrderBy(x => x.AssemblyName.Name).ToList();
                if (OnlyConflicts && assemblyInfos.Count <= 1)
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


                var referenced =
                    assemblyInfos.SelectMany(x => x.ReferencedBy).GroupBy(
                            x =>
                                x.AssemblyName.Name).Where(x => x.Key.ToUpperInvariant().StartsWith(ReferencedStartsWith.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(ReferencedStartsWith) && !referenced.Any())
                {
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(AsmSpy_CommandLine.Reference);
                Console.ForegroundColor = GetMainNameColor(assemblyInfos);
                Console.WriteLine(AsmSpy_CommandLine.ConsoleVisualizer_Visualize__0_, assemblyGroup.Key);

                foreach (var assemblyInfo in assemblyInfos)
                {
                    VisualizeAssemblyReferenceInfo(assemblyInfo);
                }

                Console.WriteLine();
            }
            Console.ResetColor();
        }

        protected virtual ConsoleColor GetMainNameColor(IList<AssemblyReferenceInfo> assemblyReferenceInfoList)
        {
            ConsoleColor mainNameColor;
            if (assemblyReferenceInfoList.Any(x => x.AssemblySource == AssemblySource.Unknown))
            {
                mainNameColor = AssemblyUnknownColor;
            }
            else if (assemblyReferenceInfoList.Any(x => x.AssemblySource == AssemblySource.NotFound))
            {
                mainNameColor = AssemblyNotFoundColor;
            }
            else if (assemblyReferenceInfoList.Any(x => x.AssemblySource == AssemblySource.GlobalAssemblyCache))
            {
                mainNameColor = AssemblyGlobalAssemblyCacheColor;
            }
            else
            {
                mainNameColor = AssemblyLocalColor;
            }
            return mainNameColor;
        }

        protected virtual void VisualizeAssemblyReferenceInfo(AssemblyReferenceInfo assemblyReferenceInfo)
        {
            if (assemblyReferenceInfo == null)
            {
                throw new ArgumentNullException(nameof(assemblyReferenceInfo));
            }
            ConsoleColor statusColor;
            switch (assemblyReferenceInfo.AssemblySource)
            {
                case AssemblySource.NotFound:
                    statusColor = AssemblyNotFoundColor;
                    break;
                case AssemblySource.Local:
                    statusColor = AssemblyLocalColor;
                    break;
                case AssemblySource.GlobalAssemblyCache:
                    statusColor = AssemblyGlobalAssemblyCacheColor;
                    break;
                case AssemblySource.Unknown:
                    statusColor = AssemblyUnknownColor;
                    break;
                default:
                    throw new InvalidEnumArgumentException(AsmSpy_CommandLine.Invalid_AssemblySource);
            }
            Console.ForegroundColor = statusColor;
            Console.WriteLine(AsmSpy_CommandLine.ConsoleVisualizer_VisualizeAssemblyReferenceInfo____0_, assemblyReferenceInfo.AssemblyName);
            Console.Write(AsmSpy_CommandLine.Source_, assemblyReferenceInfo.AssemblySource);
            if (assemblyReferenceInfo.AssemblySource != AssemblySource.NotFound)
            {
                Console.WriteLine(AsmSpy_CommandLine.Location_, assemblyReferenceInfo.ReflectionOnlyAssembly.Location);
            }
            else
            {
                Console.WriteLine();
            }

            foreach (var referer in assemblyReferenceInfo.ReferencedBy.OrderBy(x => x.AssemblyName.ToString()))
            {
                if (!string.IsNullOrEmpty(ReferencedStartsWith) && !referer.AssemblyName.Name.ToUpperInvariant().StartsWith(ReferencedStartsWith.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.ForegroundColor = statusColor;
                Console.Write(AsmSpy_CommandLine.ConsoleVisualizer_VisualizeAssemblyReferenceInfo______0_, assemblyReferenceInfo.AssemblyName.Version);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(AsmSpy_CommandLine.by);

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(AsmSpy_CommandLine.ConsoleVisualizer_VisualizeAssemblyReferenceInfo__0_, referer.AssemblyName);
            }
        }

        #endregion
    }
}
