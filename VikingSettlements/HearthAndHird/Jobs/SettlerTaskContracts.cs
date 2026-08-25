using System;
using System.Collections.Generic;
using HearthAndHird.AI;
using UnityEngine;
using VikingSettlements.Npcs;

namespace HearthAndHird.Jobs
{
    internal enum SettlerTaskStatus
    {
        Running,
        Succeeded,
        Failed,
        Blocked,
    }

    /// <summary>Runtime dependencies passed to a physical settler task.</summary>
    internal sealed class SettlerTaskContext
    {
        internal GameObject Actor { get; set; }
        internal ZNetView NetworkView { get; set; }
        internal Humanoid Humanoid { get; set; }
        internal MonsterAI Ai { get; set; }
        internal SettlerRecruitable Settler { get; set; }
        internal SettlerDirectiveState Directive { get; set; }
    }

    /// <summary>
    /// Contract for physical work. Implementations own movement, animation,
    /// interaction and hauling; they must never grant resources on a timer.
    /// </summary>
    internal interface ISettlerTask
    {
        string Id { get; }
        bool CanStart(SettlerTaskContext context);
        void Start(SettlerTaskContext context);
        SettlerTaskStatus Tick(SettlerTaskContext context, float deltaTime);
        void Cancel(SettlerTaskContext context);
    }

    /// <summary>
    /// Registration seam for incremental job ports. A registered physical
    /// task also suppresses the matching legacy timed-production switch.
    /// </summary>
    internal static class SettlerTaskRegistry
    {
        private static readonly Dictionary<string, Func<ISettlerTask>> Factories =
            new Dictionary<string, Func<ISettlerTask>>(StringComparer.OrdinalIgnoreCase);

        internal static void Register(string workId, Func<ISettlerTask> factory)
        {
            if (string.IsNullOrWhiteSpace(workId))
            {
                throw new ArgumentException("A physical task requires a stable work id.", nameof(workId));
            }
            Factories[workId] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        internal static bool HasHandler(string workId)
        {
            return !string.IsNullOrEmpty(workId) && Factories.ContainsKey(workId);
        }

        internal static bool TryCreate(string workId, out ISettlerTask task)
        {
            task = null;
            if (string.IsNullOrEmpty(workId) || !Factories.TryGetValue(workId, out var factory))
            {
                return false;
            }
            task = factory();
            return task != null;
        }
    }
}
