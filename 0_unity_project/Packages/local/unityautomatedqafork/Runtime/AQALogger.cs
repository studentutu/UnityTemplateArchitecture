using UnityEngine;
using System;

namespace Unity.AutomatedQA
{
  internal class AQALogHandler : ILogHandler
  {
    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
      Debug.unityLogger.logHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
      Debug.unityLogger.LogException(exception, context);
    }
  }
  
  public class AQALogger
  {
    public enum LogLevel
    {
      Disabled = 0,
      Error,
      Warning,
      Info,
      Debug,
    }
    
    private Logger logger = null;
    private LogLevel logLevel = LogLevel.Info;
    
    public AQALogger()
    {
      logger = new Logger(new AQALogHandler());
      logLevel = AutomatedQASettings.LogLevel;
    }

    public bool IsLogLevelEnabled(LogLevel level)
    {
      return level <= this.logLevel;
    }

    public void LogDebug(object message)
    {
      if(!IsLogLevelEnabled(LogLevel.Debug))
        return;
      
      logger.Log("AQA", $"(DEBUG) {message}");
    }

    public void Log(object message)
    {
      if(!IsLogLevelEnabled(LogLevel.Info))
        return;
      
      logger.Log("AQA", message);
    }
    
    public void LogWarning(object message)
    {
      if(!IsLogLevelEnabled(LogLevel.Warning))
        return;
      
      logger.LogWarning("AQA", message);
    }
    
    public void LogError(object message)
    {
      if(!IsLogLevelEnabled(LogLevel.Error))
        return;
      
      logger.LogError("AQA", message);
    }
    
    public void LogException(Exception ex)
    {
      logger.LogException(ex);
    }
  }
}