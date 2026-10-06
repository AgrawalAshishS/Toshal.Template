using System.Collections;

using Microsoft.Build.Framework;

namespace Toshal.Template.Compiled.Tests.Support
{
    // A build engine for running an MSBuild task in a test. It keeps the errors and messages the task logs.
    public sealed class TestBuildEngine : IBuildEngine
    {
        public List<BuildErrorEventArgs> Errors { get; } = new List<BuildErrorEventArgs>();

        public List<string> Messages { get; } = new List<string>();

        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e) => this.Errors.Add(e);

        public void LogMessageEvent(BuildMessageEventArgs e) => this.Messages.Add(e.Message ?? string.Empty);

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
        }
    }
}
