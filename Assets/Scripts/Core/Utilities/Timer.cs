using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBreaker.Utilities
{
    /// <summary>
    /// Lightweight timer system that doesn't require MonoBehaviours.
    /// </summary>
    public class Timer
    {
        private float duration;
        private float elapsed;
        private bool isRunning;
        private bool isPaused;
        private bool loops;
        private Action onComplete;
        private Action<float> onTick;

        public float Duration => duration;
        public float Elapsed => elapsed;
        public float Remaining => Mathf.Max(0, duration - elapsed);
        public float Progress => duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;
        public bool IsComplete => elapsed >= duration;

        public Timer(float duration, Action onComplete = null, bool loops = false)
        {
            this.duration = duration;
            this.onComplete = onComplete;
            this.loops = loops;
            this.elapsed = 0f;
            this.isRunning = false;
            this.isPaused = false;
        }

        public Timer OnTick(Action<float> callback)
        {
            onTick = callback;
            return this;
        }

        public Timer OnComplete(Action callback)
        {
            onComplete = callback;
            return this;
        }

        public void Start()
        {
            isRunning = true;
            isPaused = false;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        public void Stop()
        {
            isRunning = false;
            isPaused = false;
        }

        public void Reset()
        {
            elapsed = 0f;
        }

        public void Restart()
        {
            Reset();
            Start();
        }

        public bool Tick(float deltaTime)
        {
            if (!isRunning || isPaused)
                return false;

            elapsed += deltaTime;
            onTick?.Invoke(Progress);

            if (elapsed >= duration)
            {
                onComplete?.Invoke();

                if (loops)
                {
                    elapsed = 0f;
                    return false;
                }
                else
                {
                    isRunning = false;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Manages multiple timers and updates them automatically.
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        private static TimerManager instance;
        public static TimerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("TimerManager");
                    instance = go.AddComponent<TimerManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly List<Timer> activeTimers = new List<Timer>();
        private readonly List<Timer> timersToRemove = new List<Timer>();
        private readonly List<DelayedAction> delayedActions = new List<DelayedAction>();

        private struct DelayedAction
        {
            public float Delay;
            public float Elapsed;
            public Action Callback;
            public bool UseUnscaledTime;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // Update timers
            foreach (var timer in activeTimers)
            {
                if (timer.Tick(deltaTime))
                {
                    timersToRemove.Add(timer);
                }
            }

            foreach (var timer in timersToRemove)
            {
                activeTimers.Remove(timer);
            }
            timersToRemove.Clear();

            // Update delayed actions
            for (int i = delayedActions.Count - 1; i >= 0; i--)
            {
                var action = delayedActions[i];
                action.Elapsed += action.UseUnscaledTime ? Time.unscaledDeltaTime : deltaTime;

                if (action.Elapsed >= action.Delay)
                {
                    action.Callback?.Invoke();
                    delayedActions.RemoveAt(i);
                }
                else
                {
                    delayedActions[i] = action;
                }
            }
        }

        /// <summary>
        /// Creates and starts a new timer.
        /// </summary>
        public Timer CreateTimer(float duration, Action onComplete = null, bool loops = false)
        {
            var timer = new Timer(duration, onComplete, loops);
            activeTimers.Add(timer);
            timer.Start();
            return timer;
        }

        /// <summary>
        /// Removes a timer from management.
        /// </summary>
        public void RemoveTimer(Timer timer)
        {
            activeTimers.Remove(timer);
        }

        /// <summary>
        /// Executes an action after a delay.
        /// </summary>
        public void Delay(float delay, Action callback, bool useUnscaledTime = false)
        {
            delayedActions.Add(new DelayedAction
            {
                Delay = delay,
                Elapsed = 0f,
                Callback = callback,
                UseUnscaledTime = useUnscaledTime
            });
        }

        /// <summary>
        /// Cancels all pending delayed actions.
        /// </summary>
        public void CancelAllDelayed()
        {
            delayedActions.Clear();
        }

        /// <summary>
        /// Stops all active timers.
        /// </summary>
        public void StopAllTimers()
        {
            foreach (var timer in activeTimers)
            {
                timer.Stop();
            }
            activeTimers.Clear();
        }

        /// <summary>
        /// Static shorthand for creating a quick timer.
        /// </summary>
        public static Timer After(float seconds, Action callback)
        {
            return Instance.CreateTimer(seconds, callback);
        }

        /// <summary>
        /// Static shorthand for delayed action.
        /// </summary>
        public static void DoAfter(float seconds, Action callback)
        {
            Instance.Delay(seconds, callback);
        }
    }

    /// <summary>
    /// Cooldown tracker for abilities and actions.
    /// </summary>
    public class Cooldown
    {
        private float cooldownTime;
        private float lastUsedTime;

        public float CooldownTime
        {
            get => cooldownTime;
            set => cooldownTime = Mathf.Max(0, value);
        }

        public float RemainingTime => Mathf.Max(0, cooldownTime - (Time.time - lastUsedTime));
        public float Progress => cooldownTime > 0 ? Mathf.Clamp01((Time.time - lastUsedTime) / cooldownTime) : 1f;
        public bool IsReady => Time.time >= lastUsedTime + cooldownTime;

        public Cooldown(float cooldownTime)
        {
            this.cooldownTime = cooldownTime;
            this.lastUsedTime = -cooldownTime; // Start ready
        }

        /// <summary>
        /// Attempts to use the ability. Returns true if successful.
        /// </summary>
        public bool TryUse()
        {
            if (!IsReady)
                return false;

            lastUsedTime = Time.time;
            return true;
        }

        /// <summary>
        /// Forces the cooldown to be used, regardless of ready state.
        /// </summary>
        public void ForceUse()
        {
            lastUsedTime = Time.time;
        }

        /// <summary>
        /// Resets the cooldown to ready state.
        /// </summary>
        public void Reset()
        {
            lastUsedTime = -cooldownTime;
        }

        /// <summary>
        /// Reduces the remaining cooldown by the specified amount.
        /// </summary>
        public void ReduceCooldown(float amount)
        {
            lastUsedTime -= amount;
        }
    }
}
