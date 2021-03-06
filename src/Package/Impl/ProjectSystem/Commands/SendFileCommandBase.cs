﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Common.Core.IO;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Host;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.R.Package.Shell;
#if VS14
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
#endif

namespace Microsoft.VisualStudio.R.Package.ProjectSystem.Commands {
    internal class SendFileCommandBase {
        private readonly IRInteractiveWorkflowProvider _interactiveWorkflowProvider;
        private readonly IFileSystem _fs;
        private readonly IApplicationShell _appShell;

        protected SendFileCommandBase(IRInteractiveWorkflowProvider interactiveWorkflowProvider, IApplicationShell appShell, IFileSystem fs) {
            _interactiveWorkflowProvider = interactiveWorkflowProvider;
            _appShell = appShell;
            _fs = fs;
        }

        protected async Task<bool> SendToRemoteAsync(IEnumerable<string> files, string projectDir, string projectName, string remotePath, CancellationToken cancellationToken) {
            IVsStatusbar statusBar = _appShell.GetGlobalService<IVsStatusbar>(typeof(SVsStatusbar));
            return await SendToRemoteWorkerAsync(files, projectDir, projectName, remotePath, statusBar, cancellationToken);
        }

        private async Task<bool> SendToRemoteWorkerAsync(IEnumerable<string> files, string projectDir, string projectName, string remotePath, IVsStatusbar statusBar, CancellationToken cancellationToken) {
            await TaskUtilities.SwitchToBackgroundThread();

            string currentStatusText;
            statusBar.GetText(out currentStatusText);

            var workflow = _interactiveWorkflowProvider.GetOrCreate();
            var outputWindow = workflow.ActiveWindow.InteractiveWindow;
            uint cookie = 0;
            try {
                var session = workflow.RSession;
                statusBar.SetText(Resources.Info_CompressingFiles);
                statusBar.Progress(ref cookie, 1, "", 0, 0);

                int count = 0;
                uint total = (uint)files.Count() * 2; // for compressing and sending
                string compressedFilePath = string.Empty;
                await Task.Run(() => {
                    compressedFilePath = _fs.CompressFiles(files, projectDir, new Progress<string>((p) => {
                        Interlocked.Increment(ref count);
                        statusBar.Progress(ref cookie, 1, string.Format(Resources.Info_CompressingFile, Path.GetFileName(p)), (uint)count, total);
                        string dest = p.MakeRelativePath(projectDir).ProjectRelativePathToRemoteProjectPath(remotePath, projectName);
                        _appShell.DispatchOnUIThread(() => {
                            outputWindow.WriteLine(string.Format(Resources.Info_LocalFilePath, p));
                            outputWindow.WriteLine(string.Format(Resources.Info_RemoteFilePath, dest));
                        });
                    }), CancellationToken.None);
                    statusBar.Progress(ref cookie, 0, "", 0, 0);
                });

                using (var fts = new DataTransferSession(session, _fs)) {
                    cookie = 0;
                    statusBar.SetText(Resources.Info_TransferringFiles);

                    total = (uint)_fs.FileSize(compressedFilePath);

                    var remoteFile = await fts.SendFileAsync(compressedFilePath, true, new Progress<long>((b) => {
                        statusBar.Progress(ref cookie, 1, Resources.Info_TransferringFiles, (uint)b, total);
                    }), cancellationToken);

                    statusBar.SetText(Resources.Info_ExtractingFilesInRHost);
                    await session.EvaluateAsync<string>($"rtvs:::save_to_project_folder({remoteFile.Id}, {projectName.ToRStringLiteral()}, '{remotePath.ToRPath()}')", REvaluationKind.Normal, cancellationToken);

                    _appShell.DispatchOnUIThread(() => {
                        outputWindow.WriteLine(Resources.Info_TransferringFilesDone);
                    });
                }
            } catch(Exception ex) when (ex is UnauthorizedAccessException || ex is IOException) {
                _appShell.ShowErrorMessage(Resources.Error_CannotTransferFile.FormatInvariant(ex.Message));
            } catch (RHostDisconnectedException rhdex) {
                _appShell.DispatchOnUIThread(() => {
                    outputWindow.WriteErrorLine(Resources.Error_CannotTransferNoRSession.FormatInvariant(rhdex.Message));
                });
            } finally {
                statusBar.Progress(ref cookie, 0, "", 0, 0);
                statusBar.SetText(currentStatusText);
            }

            return true;
        }
    }
}
