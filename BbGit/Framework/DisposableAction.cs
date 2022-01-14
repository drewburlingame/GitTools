using System;

namespace BbGit.Framework
{
    public class DisposableAction : IDisposable
    {
        private readonly Action? _onDispose;

        public DisposableAction(Action? onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}