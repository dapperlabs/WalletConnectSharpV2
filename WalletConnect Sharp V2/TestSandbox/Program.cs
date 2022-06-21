// See https://aka.ms/new-console-template for more information

using WalletConnectSharp.Events;
using WalletConnectSharp.Events.Model;

EventDelegator events = new EventDelegator();

events.ListenFor<TestEventData>("abc", delegate(object? sender, GenericEvent<TestEventData> @event)
{
    Console.WriteLine("test1: " + @event.Response.test1);
    Console.WriteLine("test2: " + @event.Response.test2);
});

events.ListenFor<ITest>("abc", delegate(object? sender, GenericEvent<ITest> @event)
{
    Console.WriteLine("INTERFACE test1: " + @event.Response.test1);
});


events.ListenFor<TestGenericData<TestEventData>>("xyz",
    delegate(object? sender, GenericEvent<TestGenericData<TestEventData>> @event)
    {
        Console.WriteLine("GENERIC test1: " + @event.Response.data.test1);
        Console.WriteLine("GENERIC test2: " + @event.Response.data.test2);
    });

var testData1 = new TestEventData()
{
    test1 = 11,
    test2 = "abccc"
};

var testData2 = new TestGenericData<TestEventData>()
{
    data = testData1
};

Console.WriteLine("Triggering abc");
events.Trigger("abc", testData1);
Console.WriteLine("Triggering xyz");
events.Trigger("xyz", testData2);
Console.WriteLine("Triggering xyz with bad data");
events.Trigger("abc", testData2);
Console.WriteLine("Done");

interface ITest
{
    public int test1 { get; }
}

public class TestEventData : ITest
{
    public int test1 { get; set; }
    public string test2;
}

public class TestGenericData<T>
{
    public T data;
}