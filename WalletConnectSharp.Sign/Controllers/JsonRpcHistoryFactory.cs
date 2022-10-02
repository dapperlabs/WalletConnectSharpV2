using System.Collections.Generic;
using WalletConnectSharp.Core.Interfaces;

namespace WalletConnectSharp.Sign.Controllers
{
    public class JsonRpcHistoryFactory : IJsonRpcHistoryFactory
    {
        public class JsonRpcHistoryHolder<T, TR>
        {
            private static Dictionary<string, JsonRpcHistoryHolder<T, TR>> _instance;

            public static JsonRpcHistoryHolder<T, TR> InstanceForContext(ICore core)
            {
                if (_instance.ContainsKey(core.Context))
                    return _instance[core.Context];

                var historyHolder = new JsonRpcHistoryHolder<T, TR>(core);
                _instance.Add(core.Context, historyHolder);
                return historyHolder;
            }

            public IJsonRpcHistory<T, TR> History { get; }

            private JsonRpcHistoryHolder(ICore core)
            {
                History = new JsonRpcHistory<T, TR>(core);
            }
        }

        public ICore Core { get; }

        public IJsonRpcHistory<T, TR> JsonRpcHistoryOfType<T, TR>()
        {
            return JsonRpcHistoryHolder<T, TR>.InstanceForContext(Core).History;
        }
    }
}