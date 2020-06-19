// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;

    internal class CreateBundleExtensionManifestCommand
    {
        public CreateBundleExtensionManifestCommand(IntermediateSection section, WixBundleTuple bundleTuple, int lastUXPayloadIndex, string intermediateFolder, IInternalBurnBackendHelper internalBurnBackendHelper)
        {
            this.Section = section;
            this.BundleTuple = bundleTuple;
            this.LastUXPayloadIndex = lastUXPayloadIndex;
            this.IntermediateFolder = intermediateFolder;
            this.InternalBurnBackendHelper = internalBurnBackendHelper;
        }

        private IntermediateSection Section { get; }

        private WixBundleTuple BundleTuple { get; }

        private IInternalBurnBackendHelper InternalBurnBackendHelper { get; }

        private int LastUXPayloadIndex { get; }

        private string IntermediateFolder { get; }

        public WixBundlePayloadTuple BundleExtensionManifestPayloadRow { get; private set; }

        public string OutputPath { get; private set; }

        public void Execute()
        {
            this.OutputPath = this.CreateBundleExtensionManifest();

            this.BundleExtensionManifestPayloadRow = this.CreateBundleExtensionManifestPayloadRow(this.OutputPath);
        }

        private string CreateBundleExtensionManifest()
        {
            var path = Path.Combine(this.IntermediateFolder, "wix-bextdata.xml");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BundleExtensionData", BurnCommon.BundleExtensionDataNamespace);

                this.InternalBurnBackendHelper.WriteBundleExtensionData(writer);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return path;
        }

        private WixBundlePayloadTuple CreateBundleExtensionManifestPayloadRow(string bextManifestPath)
        {
            var generatedId = Common.GenerateIdentifier("ux", BurnCommon.BundleExtensionDataFileName);

            var tuple = this.Section.AddTuple(new WixBundlePayloadTuple(this.BundleTuple.SourceLineNumbers, new Identifier(AccessModifier.Private, generatedId))
            {
                Name = BurnCommon.BundleExtensionDataFileName,
                SourceFile = new IntermediateFieldPathValue { Path = bextManifestPath },
                Compressed = true,
                UnresolvedSourceFile = bextManifestPath,
                ContainerRef = BurnConstants.BurnUXContainerName,
                EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, this.LastUXPayloadIndex),
                Packaging = PackagingType.Embedded,
            });

            var fileInfo = new FileInfo(bextManifestPath);

            tuple.FileSize = (int)fileInfo.Length;

            tuple.Hash = BundleHashAlgorithm.Hash(fileInfo);

            return tuple;
        }
    }
}
