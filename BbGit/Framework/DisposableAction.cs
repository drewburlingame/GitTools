using System;

namespace BbGit.Framework
{
    public class DisposableAction : IDisposable
    {
        private readonly Action? onDispose;

        public DisposableAction(Action? onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            this.onDispose?.Invoke();
        }
    }
}