using System;
using System.Collections.Generic;
using Xunit;

namespace WalletConnectSharp.Storage.Test
{
    public class UnitTest1
    {
        [Fact]
        async public void GetSetRemoveTest()
        {
            var testDictStorage = new DictStorage();
            await testDictStorage.SetItem("somekey", "somevalue");
            Assert.Equal("somevalue",await testDictStorage.GetItem<string>("somekey"));
            await testDictStorage.RemoveItem("somekey");
            await Assert.ThrowsAsync<KeyNotFoundException>(() => testDictStorage.GetItem<string>("somekey"));
        }

        [Fact]
        async public void GetKeysTest()
        {
            var testDictStorage = new DictStorage();
            await testDictStorage.SetItem("addkey", "testingvalue");
            Assert.Equal(new string[]{"addkey"}, await testDictStorage.GetKeys()); 
        }

        [Fact]
        async public void GetEntriesTests()
        {
            var testDictStorage = new DictStorage();
            await testDictStorage.SetItem("addkey", "testingvalue");
            Assert.Equal(new object[]{"testingvalue"}, await testDictStorage.GetEntries());
            await testDictStorage.SetItem("newkey", 5);
            Assert.Equal(new int[]{5}, await testDictStorage.GetEntriesOfType<int>());

        }

        [Fact]
        async public void HasItemTest()
        {
            var testDictStorage = new DictStorage();
            await testDictStorage.SetItem("checkedkey", "testingvalue");
            Assert.True(await testDictStorage.HasItem("checkedkey"));
        }
    }
    
}