using UnityEngine;

public static class CourtyardTaskFeedback
{
    public static int CompletionCount { get; private set; }
    public static string LastTask { get; private set; }
    public static void Reset(){CompletionCount=0;LastTask=null;}
    public static void Complete(string task)
    {
        CompletionCount++;LastTask=task;
        if(!Application.isBatchMode)SoundManager.instance?.PlayEffect("quest-complete",.45f);
    }
}
