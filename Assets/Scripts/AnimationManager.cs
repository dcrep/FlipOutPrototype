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

    public void Run(System.Action onComplete = null)
    {
        if (!isRunning)
        {
            StartCoroutine(RunQueue(onComplete));
        }
    }

    public bool IsRunningAnimations()
    {
        return isRunning;
    }

    private IEnumerator RunQueue(System.Action onComplete)
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
        onComplete?.Invoke();
}

    private IEnumerator WrapWithDelay(AnimationTask task)
    {
        yield return task.Routine;              // Run the animation
        if (task.DelayAfter > 0f)               // Optional delay
            yield return new WaitForSeconds(task.DelayAfter);
    }

}
