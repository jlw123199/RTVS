﻿using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.R.Package.Commands;

namespace Microsoft.VisualStudio.R.Package.Repl.Debugger {
    internal sealed class StepIntoCommand : DebuggerWrappedCommand {
        public StepIntoCommand(IRSessionProvider rSessionProvider)
            : base(rSessionProvider, RPackageCommandId.icmdStepInto, 
                   VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.StepInto,
                   DebuggerCommandVisibility.Stopped) {
        }
    }
}