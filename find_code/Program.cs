using Swifter.RW;
using System;
using System.Collections.Generic;

namespace find_code
{
    class Program
    {

        public class Foo
        {
            [RWField("name")]
            public string Name { get; set; }
            [RWField("arg")]
            public int Age { get; set; }
        }

        public class LogOnResult
        {
            public bool Success { get; set; }
            public string Msg { get; set; }
        }


        static void Main(string[] args)
        {
            {
                var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject(new List<Foo> { new Foo { Name = "1", Age = 1 }, new Foo { Name = "2", Age = 2 } });
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<List<Foo>>(b);
            }

            {
                //var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject((true, "1 Ok"));
                var b = new byte[] { 146, 195, 164, 49, 32, 79, 107 };
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject(b, typeof((bool, string)));
            }
            {
                var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject(new Foo { Name = "1", Age = 2 });
                var d = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<Foo>(b);
                var b2 = new byte[] { 146, 161, 49, 2 };
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<Foo>(b2);
            }
            {
                var b = Swifter.Json.JsonFormatter.SerializeObject((true, "123"));
                var d2 = Swifter.Json.JsonFormatter.DeserializeObject<(bool, string)>(b);
            }
            {
                var b = new byte[] { 148, 195, 162, 111, 107, 1, 192 };
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<LogOnResult>(b);
                
            }

            Console.WriteLine("ok");
        }
    }
}
