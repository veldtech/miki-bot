using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;

namespace Miki
{
    public static class ReactiveExtensions
    {
        public static IDisposable SubscribeTask<T>(
            this IObservable<T> source, 
            Func<T, Task> onNext)
        {
            return source
                .Select(e => Observable.Defer(() => onNext(e).ToObservable()))
                .Concat()
                .Subscribe(e => { });
        }
    }
}
