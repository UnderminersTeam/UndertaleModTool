using System;

namespace UndertaleModLib.Project;

[Serializable]
public class ProjectException : Exception
{
    public ProjectException()
    {
    }

    public ProjectException(string message) : base(message)
    {
    }

    public ProjectException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

[Serializable]
public class GameFileBackupException : Exception
{
    public GameFileBackupException()
    {
    }

    public GameFileBackupException(string message) : base(message)
    {
    }

    public GameFileBackupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}