using System;
using System.Collections;
using System.Threading;

namespace SkylinesAgentBridge
{
    public sealed class CommandQueue
    {
        private readonly object gate = new object();
        // CS1 ships an older Mono runtime that can fail to load the array type
        // synthesized by Queue<T> when T is a private nested class. Keep the
        // queued command implementation private, but use the non-generic queue
        // so the runtime only needs to allocate an object[].
        private readonly Queue commands = new Queue();

        public CommandResult RunSync(Func<CommandResult> work, int timeoutMs)
        {
            QueuedCommand command = new QueuedCommand(work);

            lock (gate)
            {
                commands.Enqueue(command);
            }

            if (!command.Wait(timeoutMs))
            {
                return CommandResult.Fail("Timed out waiting for the game thread.");
            }

            return command.Result;
        }

        public void Process(int maxCount)
        {
            int processed = 0;

            while (processed < maxCount)
            {
                QueuedCommand command = null;

                lock (gate)
                {
                    if (commands.Count == 0)
                    {
                        return;
                    }

                    command = (QueuedCommand)commands.Dequeue();
                }

                command.Execute();
                processed++;
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                while (commands.Count > 0)
                {
                    ((QueuedCommand)commands.Dequeue()).Cancel("Level is unloading.");
                }
            }
        }

        private sealed class QueuedCommand
        {
            private readonly Func<CommandResult> work;
            private readonly ManualResetEvent done = new ManualResetEvent(false);
            private CommandResult result;

            public QueuedCommand(Func<CommandResult> work)
            {
                this.work = work;
            }

            public CommandResult Result
            {
                get { return result; }
            }

            public bool Wait(int timeoutMs)
            {
                return done.WaitOne(timeoutMs, false);
            }

            public void Execute()
            {
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    result = CommandResult.Fail(ex.GetType().Name + ": " + ex.Message);
                }
                finally
                {
                    done.Set();
                }
            }

            public void Cancel(string message)
            {
                result = CommandResult.Fail(message);
                done.Set();
            }
        }
    }
}
