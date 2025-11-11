using System;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : Singleton<TimerManager>
{
    private List<Task> tasks = new List<Task>();
    public List<Task> Tasks
    { get { return tasks; } }

    private void Update()
    {
        //Reverse for loop to avoid index issues when removing items from the list
        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            if (tasks[i].IsActive) tasks[i].Tick(); //Calls the tick function of the task
            else tasks.RemoveAt(i); //Removes the task from the list if it is inactive
        }
    }

    /// <summary>
    /// Initializes a new timed action
    /// </summary>
    /// <param name="duration">Duration before action will be performed</param>
    /// <param name="onComplete">Action that will be performed after duration has passed</param>
    /// <returns></returns>
    public void CreateTimedTask(float duration, Action onComplete)
    {
        tasks.Add(TimedTask.Create(duration, onComplete));
    }

    /// <summary>
    /// Initializes a new timed routine
    /// </summary>
    /// <param name="duration">Duration of the routine being performed</param>
    /// <param name="routine">Action that will be performed over the duration of the routine</param>
    /// <returns></returns>
    public void CreateRoutineTask(float duration, Action routine, Action onComplete = null)
    {
        tasks.Add(RoutineTask.Create(duration, routine, onComplete));
    }

    /// <summary>
    /// Initializes a new task sequence
    /// </summary>
    /// <param name="sequence">List of tasks to be performed</param>
    /// <returns></returns>
    public void CreateTaskSequence(Task[] sequence)
    {
        tasks.Add(TaskSequence.Create(sequence));
    }
    public void CreateTaskSequence(TaskSequence sequence)
    {
        tasks.Add(sequence);
    }
}

public abstract class Task
{
    public bool IsActive; //Whether the task is active or not
    public Task()
    {
        IsActive = true;
    }    

    //Creates a common function that will be called each frame in the update loop
    public abstract void Tick();
}
public abstract class Task<T> : Task where T: Task<T>, new ()
{
    public static Stack<T> taskPool = new Stack<T>(); //Stack to hold the objects in the pool

    public static T Get()
    {
        if(taskPool.Count > 0) //Checks if there are objects in the pool 
        {
            T result = taskPool.Pop();
            result.IsActive = true;
            return result;
        }
        else return new T();
    }
    public virtual void Return()
    {
        IsActive = false;
        taskPool.Push((T)this); //Returns the object to the pool
    }
}

public class TimedTask : Task<TimedTask>
{
    public float Duration; //Duration of the task
    public float ElapsedTime; //Current time elapsed
    public Action OnComplete;

    /// <summary>
    /// Creates a TimedAction
    /// </summary>
    /// <param name="duration">Duration before action will be performed</param>
    /// <param name="onComplete">Action that will be performed after duration has passed</param>
    /// <returns></returns>
    public static TimedTask Create(float duration, Action onComplete)
    {
        TimedTask result = Get();

        result.Duration = duration;
        result.ElapsedTime = 0f;
        result.OnComplete = onComplete;

        return result;
    }
    public override void Tick()
    {
        ElapsedTime += Time.deltaTime;
        if (ElapsedTime >= Duration)
        {
            OnComplete?.Invoke();
            Return();
        }
    }
}

/// <summary>
/// Performs actions over a certain amount of time. Use Create function to get a new instance of the class.
/// </summary>
public class RoutineTask : Task<RoutineTask>
{
    public float Duration; //Duration of the task
    public float ElapsedTime; //Current time elapsed
    public Action Routine;
    public Action OnComplete;

    /// <summary>
    /// Creates a TimedRoutine 
    /// </summary>
    /// <param name="duration">Duration of the routine being performed</param>
    /// <param name="routine">Action that will be performed over the duration of the routine</param>
    /// <returns></returns>
    public static RoutineTask Create(float duration, Action routine, Action onComplete = null)
    {
        RoutineTask result = Get();

        result.Duration = duration;
        result.ElapsedTime = 0f;
        result.Routine = routine;
        result.OnComplete = onComplete;

        return result;
    }
    public override void Tick()
    {
        ElapsedTime += Time.deltaTime;
        if (ElapsedTime <= Duration) Routine?.Invoke();
        else
        {
            OnComplete?.Invoke();
            Return();
        }
    }
    public void Stop()
    {
        Return();
    }
}

/// <summary>
/// Performs a sequence of tasks. Use Create function to get a new instance of the class.
/// </summary>
public class TaskSequence : Task<TaskSequence>
{
    private Queue<Task> taskQueue = new Queue<Task>();

    public bool IsComplete
    { get { return taskQueue.Count == 0; } }

    public static TaskSequence Create(Task sequence)
    {
        TaskSequence result = Get();

        result.taskQueue.Enqueue(sequence);
        result.IsActive = true;

        return result;
    }
    public static TaskSequence Create(Task[] sequence)
    {
        TaskSequence result = Get();


        //if (result == null) result = new TaskSequence();

        foreach (Task task in sequence)
        {
            result.taskQueue.Enqueue(task);
        }
        result.IsActive = true;

        return result;
    }
    public void AddTask(Task task) => taskQueue.Enqueue(task);
    public void AddTask(Task[] task)
    {
        foreach (Task t in task) taskQueue.Enqueue(t);
    }
    public override void Tick()
    {
        if (taskQueue.Count > 0)
        {
            Task currentTask = taskQueue.Peek();

            if (currentTask == null)
            {
                taskQueue.Dequeue();
                return;
            }

            currentTask.Tick();

            if (currentTask.IsActive == false)
            {
                taskQueue.Dequeue();
                if (taskQueue.Count == 0) Return();
            }
        }
    }
}

/// <summary>
/// Waits for a certain amount of time. Use Create function to get a new instance of the class.
/// </summary>
public class Wait : Task<Wait>
{
    public float Duration; // Duration of the task
    public float ElapsedTime; // Current time elapsed

    /// <summary>
    /// Creates a Wait
    /// </summary>
    /// <param name="duration">Duration of the wait</param>
    /// <returns></returns>
    public static Wait Create(float duration)
    {
        Wait result = Get();

        result.Duration = duration;
        result.ElapsedTime = 0f;
        return result;
    }
    public override void Tick()
    {
        ElapsedTime += Time.deltaTime;
        if (ElapsedTime >= Duration)
        {
            Return();
        }
    }
}

/// <summary>
/// Performs an action instantly. Use Create function to get a new instance of the class.
/// </summary>
public class InstantTask : Task<InstantTask>
{
    public Action OnComplete;
    public override void Tick()
    {
        OnComplete?.Invoke();
        Return();
    }

    /// <summary>
    /// Creates an InstantTask
    /// </summary>
    /// <param name="task">Task to perform</param>
    /// <returns></returns>
    public static InstantTask Create(Action task)
    {
        InstantTask result = Get();

        //if (result == null) result = new InstantTask();

        result.OnComplete = task;
        return result;
    }
}