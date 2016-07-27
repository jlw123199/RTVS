﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Common.Core.IO;
using Microsoft.Common.Core.Shell;
using Microsoft.Languages.Core.Settings;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.R.Package.ProjectSystem;
using Microsoft.VisualStudio.R.Package.Sql.Publish;
using NSubstitute;

namespace Microsoft.VisualStudio.R.Package.Test.Sql {
    [ExcludeFromCodeCoverage]
    [Category.Sql]
    public class PublishDialogTest {
        [Test(ThreadType.UI)]
        public void Constructor() {
            var coreShell = Substitute.For<ICoreShell>();
            var pss = Substitute.For<IProjectSystemServices>();
            var fs = Substitute.For<IFileSystem>();
            var storage = Substitute.For<IWritableSettingsStorage>();

            var dlg = new SqlPublshDialog(coreShell, pss, fs, storage, new string[] { @"C:\file.r", @"C:\" });
            dlg.Title.Should().Be(Resources.SqlPublishDialog_Title);
            dlg.DataContext.Should().BeOfType(typeof(SqlPublishDialogViewModel));

            var model = dlg.DataContext as SqlPublishDialogViewModel;
            model.CanGenerate.Should().BeFalse();
            model.GenerateTable.Should().BeFalse();
            model.Settings.Should().NotBeNull();
        }
    }
}
