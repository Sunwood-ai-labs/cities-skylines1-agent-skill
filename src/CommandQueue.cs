using System;
using System.Collections.Generic;
using System.Threading;

namespace SkylinesAgentBridge
{
    public sealed class CommandQueue
    {
        private readonly object gate = new object();
        private readonly Queue<QueuedCommand> commands = new Queue<QueuedCommand>();

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

                    command = commands.Dequeue();
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
                    commands.Dequeue().Cancel("Level is unloading.");
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
