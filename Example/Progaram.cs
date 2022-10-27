using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using AssemblyToProcess;

namespace Example;

[MongoIncUpdateInterface]
public interface HelloInterface
{
    protected BitArray Dirties { get; set; }

    protected void Init()
    {
        Console.WriteLine("Init");
        Console.WriteLine();
        Dirties = new BitArray(3);
    }

    static void PropChange(object? sender, string propName)
    {
        var self = sender as HelloInterface;
        self.Dirties.Set(1, true);
        Console.WriteLine(sender);
        Console.WriteLine(propName);
        Console.WriteLine();
    }
}

[MongoIncUpdate]
public class HelloWorld
{
    public int BB { get; set; }
                                    public int CC { get; set; }
                                
                                    public int? DD { get; set; }
}

class MyClass
{
    
}

public class Progaram
{
    public static void Main(string[] args)
    {
        var a = new HelloWorld();
        a.BB = 1;
        a.CC = 2;
        a.DD = null;
        Console.WriteLine();
    }
}