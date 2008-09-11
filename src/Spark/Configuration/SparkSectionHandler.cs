﻿/*
   Copyright 2008 Louis DeJardin - http://whereslou.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;

namespace Spark.Configuration
{
    public class SparkSectionHandler : ConfigurationSection, ISparkSettings
    {
        [ConfigurationProperty("compilation")]
        public CompilationElement Compilation
        {
            get { return (CompilationElement)this["compilation"]; }
            set { this["compilation"] = value; }
        }

        [ConfigurationProperty("pages")]
        public PagesElement Pages
        {
            get { return (PagesElement)this["pages"]; }
            set { this["pages"] = value; }
        }


        public SparkSectionHandler SetDebug(bool debug)
        {
            Compilation.Debug = debug;
            return this;
        }

        public SparkSectionHandler SetPageBaseType(string typeName)
        {
            Pages.PageBaseType = typeName;
            return this;
        }

        public SparkSectionHandler SetPageBaseType(Type type)
        {
            Pages.PageBaseType = type.FullName;
            return this;
        }

        public SparkSectionHandler AddAssembly(string assembly)
        {
            Compilation.Assemblies.Add(assembly);
            return this;
        }

        public SparkSectionHandler AddAssembly(Assembly assembly)
        {
            Compilation.Assemblies.Add(assembly.FullName);
            return this;
        }

        public SparkSectionHandler AddNamespace(string ns)
        {
            Pages.Namespaces.Add(ns);
            return this;
        }

        bool ISparkSettings.Debug
        {
            get { return Compilation.Debug; }
        }

        string ISparkSettings.Prefix
        {
            get { return Pages.Prefix; }
        }

        string ISparkSettings.PageBaseType
        {
            get { return Pages.PageBaseType; }
            set { Pages.PageBaseType = value; }
        }

        IList<string> ISparkSettings.UseNamespaces
        {
            get
            {
                var result = new List<string>();
                foreach (NamespaceElement ns in Pages.Namespaces)
                    result.Add(ns.Namespace);
                return result;
            }
        }

        IList<string> ISparkSettings.UseAssemblies
        {
            get
            {
                var result = new List<string>();
                foreach (AssemblyElement assembly in Compilation.Assemblies)
                    result.Add(assembly.Assembly);
                return result;
            }
        }

        public IList<ResourceMapping> ResourceMappings
        {
            get
            {
                var result = new List<ResourceMapping>();
                foreach (ResourcePathElement resource in Pages.Resources)
                    result.Add(new ResourceMapping { Match = resource.Match, Location = resource.Location });
                return result;
            }
        }
    }
}
