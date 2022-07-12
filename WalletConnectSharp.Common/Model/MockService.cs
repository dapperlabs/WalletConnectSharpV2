using System;

namespace WalletConnectSharp.Common.Model
{
    public class MockService : IModule
    {
        public string Name { get; set; }
        public string Context { get; set; }

        public MockService()
        {
            Name = "mock-service";
            Context = Guid.NewGuid().ToString();
        }
    }
}