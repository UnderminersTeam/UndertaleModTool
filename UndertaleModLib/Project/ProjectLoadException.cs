using System;

namespace UndertaleModLib.Project;

[Serializable]
internal class ProjectLoadException : Exception
{
    public ProjectLoadException()
    {
    }

    public ProjectLoadException(string message) : base(message)
    {
    }

    public ProjectLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}