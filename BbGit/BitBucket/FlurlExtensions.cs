using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommandDotNet;
using CommandDotNet.Execution;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace BbGit.BitBucket
{
    public static class FlurlExtensions
    {
        public static AppRunner GiveCancellationTokenToFlurl(this AppRunner appRunner)
        {
            FlurlHttp.GlobalSettings.HttpClientFactory = new MyDefaultHttpClientFactory();

            return appRunner.Configure(cfg =>
                cfg.UseMiddleware(SetCancellationTokenToFlurl, MiddlewareStages.PostBindValuesPreInvoke));
        }

        private static Task<int> SetCancellationTokenToFlurl(CommandContext context, ExecutionDelegate next)
        {
            MyHttpClientHandler.CancellationToken = context.CancellationToken;
            return next(context);
        }


        public class MyDefaultHttpClientFactory : DefaultHttpClientFactory
        {
            public override HttpMessageHandler CreateMessageHandler() => (HttpMessageHandler)new MyHttpClientHandler()
            {
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            };
        }

        public class MyHttpClientHandler : HttpClientHandler
        {
            internal static CancellationToken CancellationToken;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // ignore the default token
                return base.SendAsync(request, CancellationToken);
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // ignore the default token
                return base.Send(request, CancellationToken);
            }
        }
    }
}