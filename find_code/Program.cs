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


        static void Main(string[] args)
        {
            {
                var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject(new List<Foo> { new Foo { Name = "1", Age = 1 }, new Foo { Name = "2", Age = 2 } });
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<List<Foo>>(b);
            }

            {
                var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject((true, "ok",1,(object)null));
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<(bool, string,int, object)>(b);
            }
            {
                var b = Swifter.MessagePack.MessagePackFormatter.SerializeObject(new Foo { Name="1",Age=2});
                var array_str = "";
                foreach (var item in b)
                {
                    array_str += item;
                    array_str += ",";
                }
                var c = new byte[] { 148, 195, 162, 111, 107, 1, 192 };
                var d2 = Swifter.MessagePack.MessagePackFormatter.DeserializeObject<Foo>(c);
            }
        }
    }
}
