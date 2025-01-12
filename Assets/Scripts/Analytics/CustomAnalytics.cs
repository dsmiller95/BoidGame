using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using PlayFabExtensions;

namespace Analytics
{
    public static class CustomAnalytics
    {
        public static void LogLevelStart(int levelIndex)
        {
            var playerEvent = new WriteClientPlayerEventRequest
            {
                EventName = "level_start",
                Timestamp = DateTime.UtcNow,
                Body = new Dictionary<string, object>
                {
                    { "level_index", levelIndex },
                }
            };
            PlayFabExtGeneral.AttemptLogEventAsync(playerEvent).Forget();
        }
        
        public static void LogLevelCompletion(int levelIndex, int usedObstacles)
        {
            var playerEvent = new WriteClientPlayerEventRequest
            {
                EventName = "level_completion",
                Timestamp = DateTime.UtcNow,
                Body = new Dictionary<string, object>
                {
                    { "level_index", levelIndex },
                    { "used_obstacles", usedObstacles }
                }
            };
            PlayFabExtGeneral.AttemptLogEventAsync(playerEvent).Forget();
        }
        
        public static void LogLevelExit(int levelIndex)
        {
            var playerEvent = new WriteClientPlayerEventRequest
            {
                EventName = "level_exit",
                Timestamp = DateTime.UtcNow,
                Body = new Dictionary<string, object>
                {
                    { "level_index", levelIndex },
                }
            };
            PlayFabExtGeneral.AttemptLogEventAsync(playerEvent).Forget();
        }
    }
}