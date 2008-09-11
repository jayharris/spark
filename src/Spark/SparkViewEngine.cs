/*
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

using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Spark.Compiler;
using Spark.Parser;
using Spark;
using Spark.FileSystem;
using Spark.Parser.Syntax;

namespace Spark
{
    public class SparkViewEngine : ISparkViewEngine
    {
        public SparkViewEngine()
            : this(null)
        {
        }

        public SparkViewEngine(ISparkSettings settings)
        {
            Settings = settings ?? (ISparkSettings)ConfigurationManager.GetSection("spark") ?? new SparkSettings();
            SyntaxProvider = new DefaultSyntaxProvider();
            ViewActivatorFactory = new DefaultViewActivator();
        }

        public IViewFolder ViewFolder { get; set; }

        private IResourcePathManager _resourcePathManager;
        public IResourcePathManager ResourcePathManager
        {
            get
            {
                if (_resourcePathManager == null)
                    _resourcePathManager = new DefaultResourcePathManager(Settings);
                return _resourcePathManager;
            }
            set { _resourcePathManager = value; }
        }

        public ISparkExtensionFactory ExtensionFactory { get; set; }
        public IViewActivatorFactory ViewActivatorFactory { get; set; }

        public ISparkSyntaxProvider SyntaxProvider { get; set; }

        public ISparkSettings Settings { get; set; }
        public string DefaultPageBaseType { get; set; }

        public ISparkViewEntry GetEntry(SparkViewDescriptor descriptor)
        {
            var key = CreateKey(descriptor);
            return CompiledViewHolder.Current.Lookup(key);
        }

        public ISparkViewEntry CreateEntry(SparkViewDescriptor descriptor)
        {
            var key = CreateKey(descriptor);
            var entry = CompiledViewHolder.Current.Lookup(key);
            if (entry == null)
            {
                entry = CreateEntry(key);
                CompiledViewHolder.Current.Store(entry);
            }
            return entry;
        }

        public ISparkView CreateInstance(SparkViewDescriptor descriptor)
        {
            return CreateEntry(descriptor).CreateInstance();
        }



        public CompiledViewHolder.Key CreateKey(SparkViewDescriptor descriptor)
        {
            return new CompiledViewHolder.Key
                        {
                            Descriptor = descriptor
                        };
        }

        public CompiledViewHolder.Entry CreateEntry(CompiledViewHolder.Key key)
        {
            var entry = new CompiledViewHolder.Entry
                            {
                                Key = key,
                                Loader = CreateViewLoader(),
                                Compiler = CreateViewCompiler(key.Descriptor)
                            };


            var chunks = new List<IList<Chunk>>();

            foreach (var template in key.Descriptor.Templates)
                chunks.Add(entry.Loader.Load(template));

            entry.Compiler.CompileView(chunks, entry.Loader.GetEverythingLoaded());

            entry.Activator = ViewActivatorFactory.Register(entry.Compiler.CompiledType);

            return entry;
        }

        private ViewCompiler CreateViewCompiler(SparkViewDescriptor descriptor)
        {
            var pageBaseType = Settings.PageBaseType;
            if (string.IsNullOrEmpty(pageBaseType))
                pageBaseType = DefaultPageBaseType;

            return new ViewCompiler
                       {
                           BaseClass = pageBaseType,
                           Descriptor = descriptor,
                           Debug = Settings.Debug,
                           UseAssemblies = Settings.UseAssemblies,
                           UseNamespaces = Settings.UseNamespaces
                       };
        }

        private ViewLoader CreateViewLoader()
        {
            return new ViewLoader
                       {
                           ViewFolder = ViewFolder,
                           SyntaxProvider = SyntaxProvider,
                           ExtensionFactory = ExtensionFactory,
                           Prefix = Settings.Prefix
                       };
        }

        public Assembly BatchCompilation(IList<SparkViewDescriptor> descriptors)
        {
            return BatchCompilation(null /*outputAssembly*/, descriptors);
        }

        public Assembly BatchCompilation(string outputAssembly, IList<SparkViewDescriptor> descriptors)
        {
            var batch = new List<CompiledViewHolder.Entry>();
            var sourceCode = new List<string>();

            foreach (var descriptor in descriptors)
            {
                var entry = new CompiledViewHolder.Entry
                {
                    Key = CreateKey(descriptor),
                    Loader = CreateViewLoader(),
                    Compiler = CreateViewCompiler(descriptor)
                };

                var templateChunks = new List<IList<Chunk>>();
                foreach (var template in descriptor.Templates)
                    templateChunks.Add(entry.Loader.Load(template));

                entry.Compiler.GenerateSourceCode(entry.Loader.GetEverythingLoaded(), templateChunks);
                sourceCode.Add(entry.Compiler.SourceCode);

                batch.Add(entry);
            }

            var batchCompiler = new BatchCompiler{OutputAssembly = outputAssembly};

            var assembly = batchCompiler.Compile(Settings.Debug, sourceCode.ToArray());
            foreach (var entry in batch)
            {
                entry.Compiler.CompiledType = assembly.GetType(entry.Compiler.ViewClassFullName);
                entry.Activator = ViewActivatorFactory.Register(entry.Compiler.CompiledType);
                CompiledViewHolder.Current.Store(entry);
            }
            return assembly;
        }

        public IList<SparkViewDescriptor> LoadBatchCompilation(Assembly assembly)
        {
            var descriptors = new List<SparkViewDescriptor>();

            foreach (var type in assembly.GetExportedTypes())
            {
                if (!typeof(ISparkView).IsAssignableFrom(type))
                    continue;

                var attributes = type.GetCustomAttributes(typeof(SparkViewAttribute), false);
                if (attributes == null || attributes.Length == 0)
                    continue;

                var descriptor = ((SparkViewAttribute)attributes[0]).BuildDescriptor();

                var entry = new CompiledViewHolder.Entry
                                {
                                    Key = new CompiledViewHolder.Key { Descriptor = descriptor },
                                    Loader = new ViewLoader(),
                                    Compiler = new ViewCompiler { CompiledType = type },
                                    Activator = ViewActivatorFactory.Register(type)
                                };
                CompiledViewHolder.Current.Store(entry);

                descriptors.Add(descriptor);
            }

            return descriptors;
        }
    }
}
