using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationTask
{
    public IEnumerator Routine;   // The animation coroutine
    public float DelayAfter = 0f; // Optional delay after finishing
}


public class AnimationManager : MonoBehaviour
{
    private Queue<List<AnimationTask>> queue = new Queue<List<AnimationTask>>();
    private bool isRunning = false;

    System.Action onCompleteCallback = null;

    public void AddParallel(params AnimationTask[] tasks)
    {
        queue.Enqueue(new List<AnimationTask>(tasks));
    }

    public void AddParallel(List<AnimationTask> tasks)
    {
        queue.Enqueue(tasks);
    }

    public void AddSequential(AnimationTask task)
    {
        queue.Enqueue(new List<AnimationTask> { task });
    }

    public bool IsRunningAnimations()
    {
        return isRunning;
    }

    public void SetActionCompleteCallback(System.Action callback, bool ifNotRunningExecuteImmediately = false)
    {
        if (IsRunningAnimations())
            onCompleteCallback = callback;
        else if (ifNotRunningExecuteImmediately)
            callback?.Invoke();
    }

    public void Run(System.Action onComplete = null)
    {
        if (!isRunning)
        {
            if (onComplete != null)
                onCompleteCallback = onComplete;
            StartCoroutine(RunQueue());
        }
    }

    private IEnumerator RunQueue()
    {
        isRunning = true;
        while (queue.Count > 0)
        {
            var group = queue.Dequeue();
            List<Coroutine> running = new List<Coroutine>();

            foreach (var task in group)
            {
                running.Add(StartCoroutine(WrapWithDelay(task)));
            }

            foreach (var c in running)
            {
                yield return c;
            }
        }
        isRunning = false;
        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
}

    private IEnumerator WrapWithDelay(AnimationTask task)
    {
        yield return task.Routine;              // Run the animation
        if (task.DelayAfter > 0f)               // Optional delay
            yield return new WaitForSeconds(task.DelayAfter);
    }

}
