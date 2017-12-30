# Chan4Net
Inspired by the concept of the `chan` in Golang/Go. This is an C# implementation of it. The APIs and usages designs are referred to Microsoft's `BlockingCollection` class.

## APIs

### Create a chan of a type.
```csharp
var size = 2;
var chan = new Chan<int>(size); // Size must be >= 1
```

### `Add()` item
```csharp
var chan = new Chan<int>(2);
chan.Add(1);
chan.Add(2);
chan.Add(3); // Will block the current thread because the channel is full
```

### `Take()` item
```csharp
var chan = new Chan<int>(2);
chan.Add(1);
chan.Add(2);

var a = chan.Take(); // a == 1
var b = chan.Take(); // b == 2
var c = chan.Take(); // Will block the current thread because the channel is empty.
```

### `Yield()` items
```csharp
var chan = new Chan<int>(2);
chan.Add(1);
chan.Add(2);

foreach(var item in chan.Yield()) {
    Console.WriteLine(item);  // Outputs 1, 2 and then block and wait, because the channel is empty.
}

Console.WriteLine("Will never go to this line");
```

### `Close()` channel
Adding item to a closed channel will throw exception.
```csharp
var chan = new Chan<int>(2);
chan.Add(1);
chan.Close();

chan.Add(2);  // Here throws InvalidOperationException, because it cannot add item into a closed channel.
```
Taking item from a closed AND empty channel will throw exception. But it's still fine to take item from a closed channel if it isn't empty.
```csharp
var chan = new Chan<int>(2);
chan.Add(1)
chan.Close();

var a = chan.Take(); // a == 1
var b = chan.Take(); // Here throws InvalidOperationException.
```

Calling `Close()` will release the blocking `Yield()`.
```csharp
var chan = new Chan<int>(2);
chan.Add(1);
chan.Add(2);
chan.Close();

foreach(var item in chan.Yield()) {
    Console.WriteLine(item);  // Outputs 1 and 2.
}

Console.WriteLine("Done");   // Outputs "Done"
```
## Code Samples
Please read below code a psuedo code in C#. Some infinite loop and thread sleeping doesn't make sense in reality.
### `Chan` as infinite message queue - slow producer and fast consumer
```csharp
var chan = new Chan<DateTime>(2);

var producer = Task.Run(() => {
    while(true) {
        Thread.Sleep(1000);
        chan.Add(DateTime.Now);   // Add an item every second for ever.
    }
});

var consumer = Task.Run(() => {
    foreach(var item in chan.Yield()) {
        Console.WriteLine(item);  // Outputs an item once it exists in channel for every.
    }
});

Task.WaitAll(producer, consumer);
```

### `Chan` as buffer/pipeline - fast producer and slow consumer
```csharp
var chan = new Chan<int>(2);

var producer = Task.Run(() => {
    chan.Add(1);
    chan.Add(2);
    chan.Add(3);
    chan.Add(4);
    chan.Add(5);
    chan.Close();
});

var consumer = Task.Run(() => {
    foreach(var item in chan.Yield()) {
        Thread.Sleep(1000);
        Console.WriteLine(item);  // Outputs 1, 2, 3, 4, 5
    }
    // Can come to this line because the channel is closed.
});

Task.WaitAll(producer, consumer);
Console.WriteLine("Done");        // Outputs "Done"
```
### `Chan` as loadbalancer - multiple consumers
```csharp
var chan = new Chan<int>(2);

var boss = Task.Run(() => {
    while(true) {
        // Create works here
        chan.Add(0);
    }
});

var worker1 = Task.Run(() => {
    foreach(var num in chan.Yield()) {
        // Worker1 works here slowly
        Thread.Sleep(1000);
    }
});

var worker2 = Task.Run(() => {
    foreach(var num in chan.Yield()) {
        // Worker2 works here slowly
        Thread.Sleep(1000);        
    }
});

Task.WaitAll(boss, worker1, worker2);
```
### `Chan` as Pub/Sub - multiple producers and multiple consumers
```csharp
var chan = new Chan<int>(2);

var publisher1 = Task.Run(() => {
    while(true) {
        // Create works here
        chan.Add(1);
    }
});

var publisher2 = Task.Run(() => {
    while(true) {
        // Create works here
        chan.Add(2);
    }
});

var subscriber1 = Task.Run(() => {
    foreach(var num in chan.Yield()) {
        if(num == 1) {
            // Does something.
        } else {
            chan.Add(num); // Add back to channel
        }
    }
});

var subscriber2 = Task.Run(() => {
    foreach(var num in chan.Yield()) {
        if(num == 2) {
            // Does something.
        } else {
            chan.Add(num); // Add back to channel
        }      
    }
});

Task.WaitAll(publisher1, publisher2, subscriber1, subscriber2);
```

## Performance vs .NET `BlockingCollection`
The automated performance tests are included in the test project. You can run on you machine.
`Chan` beats `BlockingCollection` on my local as below.
```
Buffer size: 1, Total items: 100000, by running 10 times
Chan: 
    Average time: 74 ms (743029 ticks)
BlockingCollection: 
    Average time: 129 ms (1290903 ticks)


Buffer size: 1000, Total items: 100000, by running 10 times
Chan: 
    Average time: 69 ms (695319 ticks)
BlockingCollection: 
    Average time: 94 ms (946921 ticks)


Buffer size: 100000, Total items: 100000, by running 10 times
Chan: 
    Average time: 42 ms (429084 ticks)
BlockingCollection: 
    Average time: 72 ms (726481 ticks)
```